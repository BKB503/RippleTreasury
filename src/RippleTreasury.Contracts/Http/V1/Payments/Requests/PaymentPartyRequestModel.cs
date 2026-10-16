namespace RippleTreasury.Contracts;

/// <summary>Payer or payee details supplied when accepting a payment.</summary>
public record PaymentPartyRequestModel(
    string Reference,
    string LegalName,
    string Country,
    string Kind,
    string? GroupReference,
    string AccountReference);
