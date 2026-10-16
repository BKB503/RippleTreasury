using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

/// <summary>Mock-bank wire snapshot; keeps JSON serialization concerns outside Domain.</summary>
public record TreasurySubmissionDetails(string FundingAccountId, string FundingCurrency, string SourceCountry,
    string FundingOwnerReference, string FundingBankProviderCode, string BeneficiaryReference,
    string AccountIdentifierType, string AccountIdentifier, string? Bic, string RoutingPolicyVersion, string RoutingReason)
{
    /// <summary>Copies accepted immutable metadata, never recalculating a route for retries.</summary>
    public static TreasurySubmissionDetails? From(TreasuryPaymentDetails? t) => t is null ? null :
        new(t.FundingAccountId, t.FundingCurrency, t.SourceCountry, t.FundingOwnerReference, t.FundingBankProviderCode,
            t.BeneficiaryReference, t.AccountIdentifierType, t.AccountIdentifier, t.Bic, t.RoutingPolicyVersion, t.RoutingReason);
}
