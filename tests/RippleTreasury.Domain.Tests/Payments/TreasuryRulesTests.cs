using RippleTreasury.Domain;

namespace RippleTreasury.Domain.Tests;

public class TreasuryRulesTests
{
    [Theory]
    [InlineData("other", "company-uk", "GBP", "funding_owner_mismatch")]
    [InlineData("demo-tenant", "other", "GBP", "funding_owner_mismatch")]
    [InlineData("demo-tenant", "company-uk", "EUR", "fx_not_supported")]
    public void GivenMismatchedFunding_WhenCheckingUse_ThenRejectWithStableCode(string tenant, string owner, string currency, string code)
    {
        // Given
        var account = new FundingAccount("source", "demo-tenant", "company-uk", "GBP", "GB", "bank-a", "mock-account-source");
        // When
        var error = Assert.Throws<TreasuryValidationException>(() => account.EnsureUsable(tenant, owner, currency));
        // Then
        Assert.Equal(code, error.Code);
    }

    [Fact]
    public void GivenInactiveFunding_WhenCheckingUse_ThenRejectBeforeRouting()
    {
        var account = new FundingAccount("source", "demo-tenant", "company-uk", "GBP", "GB", "bank-a", "mock-account-source", false);
        Assert.Equal("funding_account_inactive", Assert.Throws<TreasuryValidationException>(() => account.EnsureUsable("demo-tenant", "company-uk", "GBP")).Code);
    }

    [Theory]
    [InlineData("IBAN", "GB123456", null)]
    [InlineData("TokenisedReference", "12345678", null)]
    [InlineData("TokenisedReference", "mock-account-payee\n", null)]
    [InlineData("TokenisedReference", "mock-account-payee", "BAD")]
    public void GivenUnsupportedAccountFormat_WhenCreatingBeneficiary_ThenReject(string kind, string identifier, string? bic)
    {
        Assert.Throws<TreasuryValidationException>(() => new BeneficiaryAccount("payee", kind, identifier, bic));
    }

    [Fact]
    public void GivenMatchingCurrency_WhenCorridorOrAccountDiffers_ThenCapabilityStillRejects()
    {
        var account = new FundingAccount("source", "demo-tenant", "company-uk", "EUR", "GB", "bank-a", "mock-account-source");
        var bank = new BankCapability("bank-a", 1, true, ["source"], [new("DE", "EUR")]);
        Assert.True(bank.Supports(account, new("GB", "DE", "EUR")));
        Assert.False(bank.Supports(account, new("GB", "US", "EUR")));
        Assert.False((bank with { FundingAccountIds = ["different"] }).Supports(account, new("GB", "DE", "EUR")));
        Assert.False((bank with { IsAvailable = false }).Supports(account, new("GB", "DE", "EUR")));
        Assert.True(new PaymentCorridor("GB", "DE", "EUR").IsCrossBorder);
        Assert.False(new PaymentCorridor("GB", "GB", "GBP").IsCrossBorder);
    }
}
