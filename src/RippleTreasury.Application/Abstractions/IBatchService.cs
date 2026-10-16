using RippleTreasury.Contracts;

namespace RippleTreasury.Application;

/// <summary>Application use-case boundary for validated, idempotent batch intake.</summary>
public interface IBatchService
{
    /// <summary>Validates every instruction, hashes accepted intent and atomically accepts or replays the batch.</summary>
    /// <remarks>Returns after durable intake; the background worker submits payments independently.</remarks>
    Task<PaymentBatchReceiptResponseModel> Accept(CreatePaymentBatchRequestModel input, string tenant, string key,
        string correlation, string? traceParent, CancellationToken ct);
}
