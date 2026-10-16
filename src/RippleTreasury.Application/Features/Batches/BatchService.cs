using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Orchestrates validated batch intake and idempotent replay through the persistence port; bank dispatch happens later.</summary>
public class BatchService(IBatchIntakeStore store, TreasuryInstructionFactory? treasuryFactory = null, ILogger<BatchService>? logger = null) : IBatchService
{

    /// <summary>Validates the entire request using domain rules, computes the intent hash and persists or replays one atomic batch.</summary>
    public async Task<PaymentBatchReceiptResponseModel> Accept(CreatePaymentBatchRequestModel input, string tenant, string key, string correlation, string? traceParent, CancellationToken ct)
    {
        using var scope = Log.BeginScope("TenantId={TenantId} CorrelationId={CorrelationId}", tenant, correlation);
        using var activity = Telemetry.Activities.StartActivity("batch.accept");
        Log.LogInformation("Batch intake started with {PaymentCount} payments", input.Payments?.Length ?? 0);
        try
        {
            return await AcceptCore(input, tenant, key, correlation, traceParent, ct);
        }
        catch (InputException error)
        {
            Log.LogWarning("Batch intake validation failed with {ErrorCount} errors and codes {ValidationCodes}",
                error.Errors.Count, string.Join(",", error.ValidationCodes.Values.Distinct()));
            throw;
        }
        catch (ConflictException)
        {
            Log.LogWarning("Batch intake rejected because idempotency intent conflicts");
            throw;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Log.LogDebug("Batch intake cancelled; no successful commit is asserted");
            throw;
        }
        catch (Exception error)
        {
            // Exception messages may include sensitive database or caller data.
            Log.LogError("Batch intake failed with {FailureType}; no successful commit is asserted", error.GetType().Name);
            throw;
        }
    }

    private ILogger<BatchService> Log { get; } = logger ?? NullLogger<BatchService>.Instance;

    /// <summary>Validates financial intent, replays accepted requests and persists new instructions atomically.</summary>
    private async Task<PaymentBatchReceiptResponseModel> AcceptCore(CreatePaymentBatchRequestModel input, string tenant, string key, string correlation, string? traceParent, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        var codes = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128)
            errors["Idempotency-Key"] = ["Required, maximum 128 characters."];
        if (string.IsNullOrWhiteSpace(input.ClientReference) || input.ClientReference.Length > 100)
            errors["clientReference"] = ["Required, maximum 100 characters."];
        if (input.Payments is null || input.Payments.Length < 1 || input.Payments.Length > BatchRules.MaximumPayments)
            errors["payments"] = ["Between 1 and 500 payments required."];
        if (errors.ContainsKey("payments"))
            throw new InputException(errors, codes);
        if (errors.Count == 0 && input.Payments!.Any(p => p?.Treasury is not null))
        {
            string? replayHash = null;
            try { replayHash = BatchRequestFingerprint.Create(input); }
            catch (Exception error) when (error is DomainException or OverflowException)
            {
                // Invalid intent still receives the normal indexed validation errors below.
            }
            if (replayHash is not null)
            {
                var replay = await store.FindReplay(tenant, key, replayHash, ct);
                if (replay is not null)
                {
                    Telemetry.Replayed.Add(1);
                    Log.LogInformation("Batch intake replayed {BatchId}", replay.Id);
                    return replay;
                }
            }
        }
        var batchId = Guid.NewGuid();
        var instructions = new List<PaymentInstruction>();
        var refs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (line, index) in (input.Payments ?? []).Select((p, i) => (p, i)))
        {
            try
            {
                if (line is null)
                    throw new DomainException("Payment is required.");
                BatchRules.EnsureUniquePaymentReference(refs, line.ClientReference);
                var parties = PartyInputMapper.Map(line.Parties, line.Purpose);
                var treasury = line.Treasury is null ? null : await (treasuryFactory ??
                    throw new InvalidOperationException("Treasury intake dependencies are not registered."))
                    .Create(line, parties, tenant, ct);
                var provider = treasury?.FundingBankProviderCode ?? line.Provider ?? "";
                instructions.Add(InstructionRules.Create(Guid.NewGuid(), batchId, line.ClientReference, line.Amount,
                    line.Currency, line.Beneficiary, line.DestinationCountry, provider, line.Purpose,
                    line.ExecutionDate, line.Scenario, parties, treasury));
            }
            catch (Exception e) when (e is DomainException or OverflowException)
            {
                var field = $"payments[{index}]";
                errors[field] = [e is OverflowException ? "Amount exceeds supported range." : e.Message];
                if (e is TreasuryValidationException treasuryError) codes[field] = treasuryError.Code;
            }
        }
        if (errors.Count > 0)
            throw new InputException(errors, codes);
        // Reuse the exact legacy canonical shape; new intent adds only the optional treasury object.
        var hash = BatchRequestFingerprint.Create(input);
        var result = await store.Accept(tenant, key, hash, input.ClientReference, correlation, traceParent, instructions.ToArray(), ct);
        if (result.Replayed)
            Telemetry.Replayed.Add(1);
        else
            Telemetry.AcceptedBatches.Add(1);
        Log.LogInformation("Batch intake committed {BatchId} with {PaymentCount} payments; replayed {Replayed}", result.Id, instructions.Count, result.Replayed);
        return result;
    }
}
