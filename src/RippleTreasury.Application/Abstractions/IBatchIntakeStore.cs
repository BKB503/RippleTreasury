using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Persistence boundary for audited, atomic and idempotent batch intake.</summary>
public interface IBatchIntakeStore
{
    /// <summary>Returns a committed matching replay before current reference-data availability is re-evaluated; changed intent conflicts.</summary>
    Task<PaymentBatchReceiptResponseModel?> FindReplay(string tenant, string key, string hash, CancellationToken ct);

    /// <summary>Atomically saves accepted intent, execution identities, audit and idempotency, or returns the matching prior batch.</summary>
    Task<PaymentBatchReceiptResponseModel> Accept(string tenant, string key, string hash, string reference, string correlation, string? traceParent, PaymentInstruction[] instructions, CancellationToken ct);
}
