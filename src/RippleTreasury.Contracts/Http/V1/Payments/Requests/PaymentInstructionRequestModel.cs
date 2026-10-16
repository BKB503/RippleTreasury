namespace RippleTreasury.Contracts;

/// <summary>Requested amount, currency, purpose, bank and execution date for one outbound payment.</summary>
public record PaymentInstructionRequestModel(
    string ClientReference,
    decimal Amount,
    string Currency,
    string Beneficiary,
    string DestinationCountry,
    string? Provider,
    string Purpose,
    DateOnly ExecutionDate,
    string Scenario = "accept",
    PaymentPartiesRequestModel? Parties = null,
    TreasuryPaymentRequestModel? Treasury = null);
