namespace RippleTreasury.Contracts;

/// <summary>Accepted payment intent and its current execution status, version and bank reference.</summary>
public record PaymentResponseModel(
    Guid Id,
    Guid BatchId,
    Guid ExecutionId,
    string ClientReference,
    string MinorUnits,
    string Currency,
    string Beneficiary,
    string DestinationCountry,
    string Provider,
    string Purpose,
    string Status,
    int Version,
    string? BankReference,
    string CorrelationId,
    PaymentPartiesResponseModel? Parties = null,
    DateOnly ExecutionDate = default,
    TreasuryPaymentResponseModel? Treasury = null);
