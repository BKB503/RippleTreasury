using Moq;
using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Application.Tests;

public class BankCapabilityQueriesTests
{
    [Fact]
    public void Given_UnavailableProvider_When_Listing_Then_PreserveAvailabilityAndEligibleAccountCorridors()
    {
        // Given
        var catalog = new Mock<IBankCapabilityCatalog>(MockBehavior.Strict);
        catalog.Setup(x => x.GetCapabilities()).Returns([
            new BankCapability("bank-a", 10, false, ["source"], [new("DE", "EUR")])
        ]);

        // When
        var result = new BankCapabilityQueries(catalog.Object).List();

        // Then
        var bank = Assert.Single(result);
        Assert.False(bank.IsAvailable);
        Assert.Equal("bank-a", bank.Provider);
        Assert.Equal("source", Assert.Single(bank.FundingAccountIds));
        Assert.Equal("EUR", Assert.Single(bank.Destinations).Currency);
        catalog.Verify(x => x.GetCapabilities(), Times.Once());
        catalog.VerifyNoOtherCalls();
    }
}
