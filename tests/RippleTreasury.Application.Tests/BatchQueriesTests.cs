using Moq;
using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Application.Tests;

public class BatchQueriesTests
{
    [Fact]
    public async Task Given_InvalidPaging_When_ListingBatches_Then_RejectBeforeReadingTheRepository()
    {
        // Given
        var repository = new Mock<IPaymentReadRepository>(MockBehavior.Strict);
        var query = new BatchQueries(repository.Object);

        // When
        var list = () => query.Batches(0, 20, null, default);

        // Then
        await Assert.ThrowsAsync<DomainException>(list);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Given_MixedCurrenciesAndOutcomes_When_ViewingBatch_Then_KeepInstructionTotalsSeparateFromResults()
    {
        // Given
        var batch = new PaymentBatch { Id = Guid.NewGuid(), ClientReference = "payroll-2026-10", Tenant = "customer" };
        var instructions = new[] { (10m, "GBP"), (20m, "GBP"), (30m, "EUR") }.Select(x =>
            new PaymentInstruction(Guid.NewGuid(), batch.Id, Guid.NewGuid().ToString(), Money.FromDecimal(x.Item1, x.Item2),
                "Vendor Ltd", "GB", "bank-a", PaymentPurpose.VendorDisbursement, new(2026, 10, 15), "accept")).ToArray();
        var executions = instructions.Select(x => new PaymentExecution(x.Id, Guid.NewGuid())).ToArray();
        foreach (var execution in executions) execution.Claim(0);
        executions[0].ObserveAccepted(1, "bank-confirmed");
        executions[1].ObserveRejected(1, "bank-rejected");
        executions[2].MarkUnknown(1, "response lost");
        var repository = new Mock<IPaymentReadRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetBatch(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new BatchReadData(batch, instructions, executions));

        // When
        var view = await new BatchQueries(repository.Object).Batch(batch.Id, default);

        // Then: totals describe accepted instructions, including rejected/uncertain execution results.
        Assert.Equal("ActionRequired", view.Status);
        Assert.Equal("3000", view.Totals.Single(x => x.Currency == "GBP").MinorUnits);
        Assert.Equal("3000", view.Totals.Single(x => x.Currency == "EUR").MinorUnits);
        Assert.Equal(3, view.PaymentCount);
    }

}
