using Moq;
using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Application.Tests;

public class BankRoutingPolicyTests
{
    [Fact]
    public void Given_TwoEurBanks_When_SourceBelongsToOne_Then_SelectByAccountAndCorridorNotCurrencyAlone()
    {
        // Given bank-b has higher priority but cannot debit the bank-a source account.
        var source = Source();
        var policy = new BankRoutingPolicy(Capabilities(
            new("bank-b", 1, true, [source.Id], [new("DE", "EUR")]),
            new("bank-a", 2, true, [source.Id], [new("DE", "EUR")])));
        // When
        var result = policy.Select(source, new("GB", "DE", "EUR"), null);
        // Then
        Assert.Equal("bank-a", result.Provider);
        Assert.Equal("mock-routing-v1", result.PolicyVersion);
        Assert.Contains("Configured priority", result.Reason);
        Assert.Equal("routing_unsupported", Assert.Throws<TreasuryValidationException>(() =>
            policy.Select(source, new("GB", "DE", "EUR"), "bank-b")).Code);
    }

    private static FundingAccount Source() => new("source", "demo-tenant", "company-uk", "EUR", "GB", "bank-a", "mock-account-source");

    private static IBankCapabilityCatalog Capabilities(params BankCapability[] values)
    {
        var catalog = new Mock<IBankCapabilityCatalog>(MockBehavior.Strict);
        catalog.Setup(x => x.GetCapabilities()).Returns(values);
        return catalog.Object;
    }
}
