using Moq;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application.Tests;

public class TreasuryInstructionFactoryTests
{
    [Fact]
    public async Task Given_UnknownFundingAccount_When_CreatingInstruction_Then_RejectBeforeRouting()
    {
        // Given
        var accounts = new Mock<IFundingAccountRepository>(MockBehavior.Strict);
        var routing = new Mock<IBankRoutingPolicy>(MockBehavior.Strict);
        using var cancellation = new CancellationTokenSource();
        accounts.Setup(x => x.Find("source", "tenant-a", cancellation.Token)).ReturnsAsync((FundingAccount?)null);
        var line = new PaymentInstructionRequestModel("payment", 10m, "GBP", "Vendor", "GB", null,
            "VendorDisbursement", new(2026, 10, 15), Treasury: new("source", new("payee", "TokenisedReference", "mock-account-payee")));

        // When
        var create = () => new TreasuryInstructionFactory(accounts.Object, routing.Object).Create(line, null, "tenant-a", cancellation.Token);

        // Then
        var error = await Assert.ThrowsAsync<TreasuryValidationException>(create);
        Assert.Equal("funding_account_unknown", error.Code);
        accounts.Verify(x => x.Find("source", "tenant-a", cancellation.Token), Times.Once());
        routing.VerifyNoOtherCalls();
    }
}
