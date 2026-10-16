using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Maps accepted treasury intent without deriving new routing or funding decisions.</summary>
internal static class TreasuryPaymentResponseMapper
{
    /// <summary>Returns the immutable treasury snapshot, or null for preserved legacy v1 instructions.</summary>
    public static TreasuryPaymentResponseModel? Map(PaymentInstruction instruction)
    {
        var t = instruction.Treasury;
        return t is null ? null : new(t.FundingAccountId, t.FundingCurrency, t.SourceCountry, t.BeneficiaryReference,
            t.AccountIdentifierType, t.AccountIdentifier, t.Bic, t.SourceCountry != instruction.DestinationCountry,
            t.RoutingPolicyVersion, t.RoutingReason);
    }
}
