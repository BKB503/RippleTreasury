using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Integration.Tests;

public class QueryAndRetryRecoveryTests
{
    [Fact]
    public async Task Given_TwoUnknownBatches_When_QueryingCasesForOne_Then_ExcludeTheOtherBatch()
    {
        // Given
        await using var fixture = new Fixture();
        await fixture.Initialize();
        var first = await fixture.Accept(fixture.Input("lost-response"));
        await fixture.Accept(fixture.Input("lost-response") with { ClientReference = "second-batch" }, "second-key");
        await fixture.Processor.DispatchOne("worker", default);
        await fixture.Processor.DispatchOne("worker", default);
        using var factory = new ApiFactory(fixture.Path);
        using var client = factory.CreateClient();

        // When
        var scoped = await client.GetFromJsonAsync<PagedResponseModel<ReconciliationCaseResponseModel>>($"/api/v1/reconciliation-cases?batchId={first.Id}");

        // Then
        var item = Assert.Single(scoped!.Items);
        var detail = await fixture.Store.Payment(item.PaymentId, default);
        Assert.Equal(first.Id, detail.Payment.BatchId);
        Assert.Equal(1, scoped.Total);
        Assert.Equal(2, (await fixture.Store.Cases(1, 20, null, default)).Total);
    }

    [Fact]
    public async Task Given_AResolvedCaseAndLaterRetryCrash_When_Recovering_Then_ReopenTheSameCaseWithoutDuplicateExecution()
    {
        // Given: the first uncertain result has authoritative proof that no execution occurred.
        await using var fixture = new Fixture();
        await fixture.Initialize();
        await fixture.Accept(fixture.Input("accept"));
        var first = (await fixture.Store.Claim("worker", fixture.Clock.Now.UtcDateTime, TimeSpan.FromSeconds(30), default))!;
        await fixture.Store.Complete(first, new(BankOutcome.Unknown, "result unavailable"), fixture.Clock.Now.UtcDateTime, default);
        var originalCase = Assert.Single((await fixture.Store.Cases(1, 20, null, default)).Items);
        var lookup = await fixture.Store.BeginRecheck(originalCase.Id, originalCase.Version, Guid.NewGuid(), fixture.Clock.Now.UtcDateTime, default);
        await fixture.Store.FinishRecheck(lookup, new(BankOutcome.ProvenNotExecuted, "authoritative no-execution proof"), fixture.Clock.Now.UtcDateTime, default);
        fixture.Clock.Now = fixture.Clock.Now.AddMinutes(1);
        fixture.Fault.Operation = "complete";
        await Assert.ThrowsAsync<IOException>(() => fixture.Processor.DispatchOne("worker", default));
        fixture.Clock.Now = fixture.Clock.Now.AddMinutes(1);

        // When: the later submission did execute, but its local result commit was lost.
        await fixture.Processor.Recover(default);
        var reopened = await fixture.Store.Case(originalCase.Id, default);
        await fixture.Reconciler.Recheck(originalCase.Id, reopened.Case.Version, Guid.NewGuid(), default);

        // Then
        Assert.Equal(1, (await fixture.Store.Cases(1, 20, null, default)).Total);
        Assert.Equal("Accepted", (await fixture.Store.Payment(first.Instruction.Id, default)).Payment.Status);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal(1, (await db.SimulatedBankExecutions.SingleAsync()).FinancialExecutions);
        Assert.Equal(2, await db.PaymentAttempts.CountAsync(x => x.Kind == "Submit"));
    }
}
