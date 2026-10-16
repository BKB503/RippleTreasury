using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Read-side persistence port. Implementations return Domain data, never EF queries or DbContext.</summary>
public interface IPaymentReadRepository
{
    /// <summary>Loads detached batch, instruction and execution data consistently for the requested receipt-date range.</summary>
    Task<BatchReadData[]> GetBatches(DateTime? from, DateTime? to, CancellationToken ct);

    /// <summary>Loads one batch and its instructions/executions as a consistent detached read snapshot.</summary>
    Task<BatchReadData> GetBatch(Guid id, CancellationToken ct);

    /// <summary>Loads a consistent detached payment snapshot including batch, execution, attempts and audit.</summary>
    Task<PaymentReadData> GetPayment(Guid id, CancellationToken ct);

    /// <summary>Loads detached investigations and their correlation identifiers, optionally filtered by status and batch.</summary>
    Task<(ReconciliationCase Case, string CorrelationId)[]> GetCases(string? status, Guid? batchId, CancellationToken ct);

    /// <summary>Loads one investigation and its linked payment evidence within the same read transaction.</summary>
    Task<(ReconciliationCase Case, PaymentReadData Payment)> GetCase(Guid id, CancellationToken ct);
}
