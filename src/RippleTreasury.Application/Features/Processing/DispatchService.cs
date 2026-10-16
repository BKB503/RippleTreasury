using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Coordinates one durable claim, one bank submission and one result commit.</summary>
public class DispatchService
{
    private readonly IExecutionStore _store;
    private readonly IBankGateway _bank;
    private readonly TimeProvider _clock;
    private readonly ILogger<DispatchService> _logger;

    /// <summary>Connects the durable execution store, bank gateway, clock and structured logger.</summary>
    public DispatchService(IExecutionStore store, IBankGateway bank, TimeProvider clock,
        ILogger<DispatchService>? logger = null)
    {
        _store = store;
        _bank = bank;
        _clock = clock;
        _logger = logger ?? NullLogger<DispatchService>.Instance;
    }

    /// <summary>Claims one eligible payment durably, submits once and commits the observed outcome; returns false when no work is eligible.</summary>
    public async Task<bool> DispatchOne(string owner, CancellationToken ct)
    {
        // The store commits the claim/attempt before any HTTP request can leave the process.
        var item = await _store.Claim(owner, _clock.GetUtcNow().UtcDateTime, TimeSpan.FromSeconds(30), ct);

        if (item is null)
        {
            return false;
        }

        using var scope = WorkLogScope.Begin(_logger, item);
        using var activity = WorkTelemetry.Start("payment.submit", item);

        _logger.LogInformation("PaymentClaimed Owner={Owner} AttemptNumber={AttemptNumber}", owner, item.AttemptNumber);

        var started = Stopwatch.GetTimestamp();
        BankObservation observation;

        try
        {
            observation = await _bank.Submit(item, ct);
        }
        catch (Exception error) when (error is HttpRequestException or TimeoutException or OperationCanceledException)
        {
            // A lost response may follow bank execution. Record uncertainty, never resend here.
            observation = new(BankOutcome.Unknown, "Transport result ambiguous");
            _logger.LogWarning("SubmissionTransportAmbiguous FailureType={FailureType}", error.GetType().Name);
        }

        Telemetry.BankLatency.Record(
            Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            new KeyValuePair<string, object?>("provider", item.Instruction.Provider));

        // Log the recorded outcome only after state and its business audit commit together.
        try
        {
            await _store.Complete(item, observation, _clock.GetUtcNow().UtcDateTime, ct);
        }
        catch (Exception error)
        {
            _logger.LogError("PaymentOutcomeCommitFailed FailureType={FailureType}; durable attempt requires recovery", error.GetType().Name);
            throw;
        }

        Telemetry.Outcomes.Add(1, new KeyValuePair<string, object?>("outcome", observation.Outcome.ToString()));
        _logger.LogInformation("PaymentOutcomeRecorded Outcome={Outcome}", observation.Outcome);

        return true;
    }

    /// <summary>Recovers expired submission or lookup leases without treating uncertainty as permission to resend.</summary>
    public async Task<int> Recover(CancellationToken ct)
    {
        // Expired submissions become uncertain; an expired lease is not permission to retry.
        var recovered = await _store.RecoverExpired(_clock.GetUtcNow().UtcDateTime, ct);

        if (recovered > 0)
        {
            _logger.LogWarning("ExpiredSubmissionsRecovered Count={Count}", recovered);
        }

        return recovered;
    }
}
