namespace RippleTreasury.Contracts;

/// <summary>Investigation identity, linked payment, current status and concurrency version.</summary>
public record ReconciliationCaseResponseModel(
    Guid Id,
    Guid PaymentId,
    string Status,
    int Version,
    DateTime OpenedAt,
    string CorrelationId);
