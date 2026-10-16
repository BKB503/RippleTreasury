namespace RippleTreasury.Contracts;

/// <summary>Accepted payer or payee details returned by payment queries.</summary>
public record PaymentPartyResponseModel(
    string Reference,
    string LegalName,
    string Country,
    string Kind,
    string? GroupReference,
    string AccountReference);
