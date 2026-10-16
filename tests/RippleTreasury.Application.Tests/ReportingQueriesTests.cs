using Moq;
using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Application.Tests;

public class ReportingQueriesTests
{
    [Fact]
    public async Task Given_AnUnsupportedGrouping_When_RequestingReport_Then_RejectBeforeReadingTheRepository()
    {
        // Given
        var repository = new Mock<IPaymentReadRepository>(MockBehavior.Strict);
        var query = new ReportingQueries(repository.Object);

        // When
        var report = () => query.Report(null, null, "unsupported", default);

        // Then
        await Assert.ThrowsAsync<DomainException>(report);
        repository.VerifyNoOtherCalls();
    }

}
