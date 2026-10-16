namespace RippleTreasury.Domain;

/// <summary>Fictional corporate source-account reference; contains no balance, credentials or funds reservation.</summary>
public class FundingAccount
{
    /// <summary>Creates source-account reference data without balances or authorization credentials.</summary>
    private FundingAccount() { }

    /// <summary>Creates source-account reference data without balances or authorization credentials.</summary>
    public FundingAccount(string id, string tenantId, string ownerReference, string currencyCode,
        string country, string bankProviderCode, string accountReference, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(ownerReference))
            throw new TreasuryValidationException("funding_account_invalid", "Funding account identity and owner are required.");
        _ = new Money(1, currencyCode);
        if (!InstructionRules.SupportsCountry(country))
            throw new TreasuryValidationException("funding_country_invalid", "Unsupported funding account country.");
        Id = id; TenantId = tenantId; OwnerReference = ownerReference; CurrencyCode = currencyCode;
        Country = country; BankProviderCode = bankProviderCode; AccountReference = accountReference; IsActive = isActive;
    }

    public string Id { get; private set; } = "";
    public string TenantId { get; private set; } = "";
    public string OwnerReference { get; private set; } = "";
    public string CurrencyCode { get; private set; } = "";
    public string Country { get; private set; } = "";
    public string BankProviderCode { get; private set; } = "";
    public string AccountReference { get; private set; } = "";
    public bool IsActive { get; private set; }

    /// <summary>Requires an active source account owned by the submitting tenant/company, with no implicit FX.</summary>
    public void EnsureUsable(string tenant, string ownerReference, string currency)
    {
        if (TenantId != tenant || OwnerReference != ownerReference)
            throw new TreasuryValidationException("funding_owner_mismatch", "Funding account does not belong to the submitting tenant and payer company.");
        if (!IsActive) throw new TreasuryValidationException("funding_account_inactive", "Funding account is inactive.");
        if (CurrencyCode != currency)
            throw new TreasuryValidationException("fx_not_supported", "Funding and instructed currencies must match; FX conversion is not implemented.");
    }
}
