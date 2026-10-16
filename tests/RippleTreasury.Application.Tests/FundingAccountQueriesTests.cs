using Moq;
using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Application.Tests;

public class FundingAccountQueriesTests
{
    [Fact]
    public async Task Given_TenantAccounts_When_Listing_Then_ForwardTenantAndCancellationAndReturnSafeMetadata()
    {
        // Given
        using var cancellation = new CancellationTokenSource();
        var repository = new Mock<IFundingAccountRepository>(MockBehavior.Strict);
        repository.Setup(x => x.List("tenant-a", cancellation.Token)).ReturnsAsync([
            new FundingAccount("source", "tenant-a", "company", "GBP", "GB", "bank-a", "private-account-reference")
        ]);

        // When
        var result = await new FundingAccountQueries(repository.Object).List("tenant-a", cancellation.Token);

        // Then
        var account = Assert.Single(result);
        Assert.Equal("source", account.Id);
        Assert.Equal("company", account.OwnerReference);
        Assert.Equal("GBP", account.Currency);
        Assert.DoesNotContain("private-account-reference", System.Text.Json.JsonSerializer.Serialize(result));
        repository.Verify(x => x.List("tenant-a", cancellation.Token), Times.Once());
        repository.VerifyNoOtherCalls();
    }
}
