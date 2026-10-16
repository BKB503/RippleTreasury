namespace RippleTreasury.Contracts;

/// <summary>Accepted funding, typed beneficiary and routing evidence; null on unchanged legacy instructions.</summary>
public record TreasuryPaymentResponseModel(
    string FundingAccountId,
    string FundingCurrency,
    string SourceCountry,
    string BeneficiaryReference,
    string AccountIdentifierType,
    string AccountIdentifier,
    string? Bic,
    bool IsCrossBorder,
    string RoutingPolicyVersion,
    string RoutingReason);
