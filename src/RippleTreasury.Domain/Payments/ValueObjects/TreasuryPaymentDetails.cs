namespace RippleTreasury.Domain;

/// <summary>Accepted funding, beneficiary and routing snapshot. Later reference-data changes cannot rewrite payment intent.</summary>
public class TreasuryPaymentDetails
{
    /// <summary>Captures the accepted source, beneficiary and routing evidence; the private constructor supports persistence.</summary>
    private TreasuryPaymentDetails() { }

    /// <summary>Captures the accepted source, beneficiary and routing evidence; the private constructor supports persistence.</summary>
    public TreasuryPaymentDetails(FundingAccount funding, BeneficiaryAccount beneficiary, BankRoutingDecision decision)
    {
        if (decision.Provider != funding.BankProviderCode)
            throw new TreasuryValidationException("funding_provider_mismatch", "Selected bank cannot debit this source account.");
        FundingBankProviderCode = funding.BankProviderCode;
        FundingAccountId = funding.Id; FundingCurrency = funding.CurrencyCode; SourceCountry = funding.Country;
        FundingOwnerReference = funding.OwnerReference; BeneficiaryReference = beneficiary.Reference;
        AccountIdentifierType = beneficiary.IdentifierType; AccountIdentifier = beneficiary.Identifier; Bic = beneficiary.Bic;
        RoutingPolicyVersion = decision.PolicyVersion; RoutingReason = decision.Reason;
    }

    public string FundingBankProviderCode { get; private set; } = "";
    public string FundingAccountId { get; private set; } = "";
    public string FundingCurrency { get; private set; } = "";
    public string SourceCountry { get; private set; } = "";
    public string FundingOwnerReference { get; private set; } = "";
    public string BeneficiaryReference { get; private set; } = "";
    public string AccountIdentifierType { get; private set; } = "";
    public string AccountIdentifier { get; private set; } = "";
    public string? Bic { get; private set; }
    public string RoutingPolicyVersion { get; private set; } = "";
    public string RoutingReason { get; private set; } = "";

    /// <summary>Checks source/payout currency and the existing immutable payer/payee snapshots for consistency.</summary>
    public void EnsureConsistent(Money amount, PaymentParties? parties)
    {
        if (FundingCurrency != amount.Currency)
            throw new TreasuryValidationException("fx_not_supported", "Funding and instructed currencies must match.");
        if (parties is null || parties.Payer.Reference != FundingOwnerReference ||
            parties.Payer.Country != SourceCountry || parties.Payee.Reference != BeneficiaryReference ||
            parties.Payee.AccountReference != AccountIdentifier)
            throw new TreasuryValidationException("treasury_party_mismatch", "Treasury funding and beneficiary details must match the payer/payee snapshots.");
    }
}
