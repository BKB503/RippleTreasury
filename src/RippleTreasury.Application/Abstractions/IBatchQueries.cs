using RippleTreasury.Contracts;

namespace RippleTreasury.Application;

/// <summary>Read-only application boundary for batch and payment-list responses.</summary>
public interface IBatchQueries
{
    /// <summary>Returns one batch's metadata, aggregate progress and per-currency instruction totals.</summary>
    Task<PaymentBatchResponseModel> Batch(Guid id, CancellationToken ct);

    /// <summary>Returns a page of batch summaries, optionally filtered by status.</summary>
    Task<PagedResponseModel<PaymentBatchResponseModel>> Batches(int page, int size, string? status, CancellationToken ct);

    /// <summary>Returns a page of payments belonging to the specified batch.</summary>
    Task<PagedResponseModel<PaymentResponseModel>> Payments(Guid batchId, int page, int size, CancellationToken ct);
}
