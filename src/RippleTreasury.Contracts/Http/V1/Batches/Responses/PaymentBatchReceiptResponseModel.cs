namespace RippleTreasury.Contracts;

/// <summary>Accepted batch identity and indication that an idempotent request was replayed.</summary>
public record PaymentBatchReceiptResponseModel(
    Guid Id,
    bool Replayed);
