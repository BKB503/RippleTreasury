using Moq;
using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Application.Tests;

public class ReconciliationQueriesTests
{
    [Fact]
    public async Task Given_InvalidPaging_When_ListingCases_Then_RejectWithoutReadingTheRepository()
    {
        // Given
        var repository = new Mock<IPaymentReadRepository>(MockBehavior.Strict);
        var queries = new ReconciliationQueries(repository.Object);

        // When
        var list = () => queries.Cases(0, 20, null, default);

        // Then
        await Assert.ThrowsAsync<DomainException>(list);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Given_BatchAndStatusFilters_When_ListingCases_Then_ForwardFiltersAndReturnEmptyPage()
    {
        // Given
        var batchId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var repository = new Mock<IPaymentReadRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetCases("Open", batchId, cancellation.Token))
            .ReturnsAsync(Array.Empty<(ReconciliationCase Case, string CorrelationId)>());

        // When
        var result = await new ReconciliationQueries(repository.Object).Cases(1, 20, "Open", cancellation.Token, batchId);

        // Then
        Assert.Empty(result.Items);
        repository.Verify(x => x.GetCases("Open", batchId, cancellation.Token), Times.Once());
        repository.VerifyNoOtherCalls();
    }
}
