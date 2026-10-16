namespace RippleTreasury.Contracts;

/// <summary>Beneficiary identity and an explicitly typed fictional account token; raw account credentials are not accepted.</summary>
public record BeneficiaryRequestModel(
    string Reference,
    string AccountIdentifierType,
    string AccountIdentifier,
    string? Bic = null);
