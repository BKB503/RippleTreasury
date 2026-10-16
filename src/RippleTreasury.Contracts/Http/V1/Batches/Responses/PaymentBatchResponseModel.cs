namespace RippleTreasury.Contracts;

/// <summary>Batch metadata, domain-derived progress and instruction totals separated by currency.</summary>
public record PaymentBatchResponseModel(
    Guid Id,
    string ClientReference,
    string CorrelationId,
    DateTime ReceivedAt,
    string Status,
    int PaymentCount,
    CurrencyTotalResponseModel[] Totals);
