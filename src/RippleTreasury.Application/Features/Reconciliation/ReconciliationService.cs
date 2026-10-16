using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Queries provider evidence for an uncertain payment without submitting a transfer.</summary>
public class ReconciliationService
{
    private readonly IReconciliationStore _store;
    private readonly IBankGateway _bank;
    private readonly TimeProvider _clock;
    private readonly ILogger<ReconciliationService> _logger;

    /// <summary>Connects the reconciliation store, provider lookup gateway, clock and logger.</summary>
    public ReconciliationService(IReconciliationStore store, IBankGateway bank, TimeProvider clock,
        ILogger<ReconciliationService>? logger = null)
    {
        _store = store;
        _bank = bank;
        _clock = clock;
        _logger = logger ?? NullLogger<ReconciliationService>.Instance;
    }

    /// <summary>Persists a version-checked lookup attempt, queries provider evidence and atomically records the resulting domain decision.</summary>
    public async Task Recheck(Guid caseId, int expectedVersion, Guid commandId, CancellationToken ct)
    {
        _logger.LogInformation("ReconciliationCommandReceived CaseId={CaseId} CommandId={CommandId} ExpectedVersion={ExpectedVersion}", caseId, commandId, expectedVersion);

        // Persist the command, expected version and initiating actor before the provider lookup.
        var item = await _store.BeginRecheck(caseId, expectedVersion, commandId, _clock.GetUtcNow().UtcDateTime, ct);

        using var scope = WorkLogScope.Begin(_logger, item);
        using var activity = WorkTelemetry.Start("payment.reconcile", item);

        _logger.LogInformation("ReconciliationLookupStarted CaseId={CaseId} CommandId={CommandId}", caseId, commandId);

        BankObservation result;

        try
        {
            result = await _bank.Lookup(item, ct);
        }
        catch (Exception error) when (error is HttpRequestException or TimeoutException or OperationCanceledException)
        {
            result = new(BankOutcome.Unknown, "Status lookup unavailable");
            _logger.LogWarning("ReconciliationLookupUnavailable FailureType={FailureType}", error.GetType().Name);
        }

        // The durable attempt retains the original operator even if request context changes.
        try
        {
            await _store.FinishRecheck(item, result, _clock.GetUtcNow().UtcDateTime, ct);
        }
        catch (Exception error)
        {
            _logger.LogError("ReconciliationObservationCommitFailed CaseId={CaseId} FailureType={FailureType}", caseId, error.GetType().Name);
            throw;
        }

        _logger.LogInformation("ReconciliationObservationRecorded CaseId={CaseId} Outcome={Outcome}", caseId, result.Outcome);
    }
}
