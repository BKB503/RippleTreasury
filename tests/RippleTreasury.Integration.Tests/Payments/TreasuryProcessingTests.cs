using Moq;
using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public class TreasuryProcessingTests
{
    [Fact]
    public async Task GivenAcceptedTreasuryIntent_WhenBankBecomesUnavailable_ThenReplayOriginalAndRejectOnlyNewRouting()
    {
        await using var fixture = new Fixture();
        await fixture.Initialize();
        var input = TreasuryData.Batch(fixture, 1);
        var receipt = await fixture.Accept(input);
        var disabledCapabilities = new Mock<IBankCapabilityCatalog>(MockBehavior.Strict);
        disabledCapabilities.Setup(x => x.GetCapabilities()).Returns(
            new DemoBankCapabilityCatalog().GetCapabilities().Select(bank => bank with { IsAvailable = false }).ToArray());
        var unavailable = new BatchService(fixture.Store, new TreasuryInstructionFactory(new FundingAccountRepository(fixture.Factory),
            new BankRoutingPolicy(disabledCapabilities.Object)));
        var replay = await unavailable.Accept(input, "demo-tenant", "key", "later-correlation", null, default);
        Assert.True(replay.Replayed);
        Assert.Equal(receipt.Id, replay.Id);
        var error = await Assert.ThrowsAsync<InputException>(() => unavailable.Accept(input with { ClientReference = "new" },
            "demo-tenant", "new-key", "corr", null, default));
        Assert.Equal("routing_unsupported", error.ValidationCodes["payments[0]"]);
        disabledCapabilities.Verify(x => x.GetCapabilities(), Times.Once());
        var changed = input with { Payments = [input.Payments[0] with { Amount = 20m }] };
        await Assert.ThrowsAsync<ConflictException>(() => unavailable.Accept(changed, "demo-tenant", "key", "corr", null, default));
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Single(await db.PaymentInstructions.ToArrayAsync());
        Assert.Empty(await db.PaymentAttempts.ToArrayAsync());
        Assert.Equal("bank-a", (await db.PaymentInstructions.SingleAsync()).Provider);
    }

    [Fact]
    public async Task GivenBankBThrottleProof_WhenRetryIsDue_ThenReuseTheOriginalExecutionIdentity()
    {
        await using var fixture = new Fixture();
        await fixture.Initialize();
        var batch = TreasuryData.Batch(fixture, 3);
        await fixture.Accept(batch with { Payments = [batch.Payments[2] with { Scenario = "rate-limit" }] });
        Assert.True(await fixture.Processor.DispatchOne("first", default));
        Assert.False(await fixture.Processor.DispatchOne("early", default));
        await using (var db = fixture.Factory.CreateDbContext())
        {
            Assert.Empty(await db.SimulatedBankExecutions.ToArrayAsync());
            Assert.Equal(PaymentStatus.RetryScheduled, (await db.PaymentExecutions.SingleAsync()).Status);
        }
        fixture.Clock.Now = fixture.Clock.Now.AddSeconds(3);
        Assert.True(await fixture.Processor.DispatchOne("retry", default));
        await using var check = fixture.Factory.CreateDbContext();
        Assert.Equal(2, await check.PaymentAttempts.CountAsync());
        Assert.Equal((await check.PaymentExecutions.SingleAsync()).ExecutionId, (await check.SimulatedBankExecutions.SingleAsync()).Id);
        Assert.Equal(1, (await check.SimulatedBankExecutions.SingleAsync()).FinancialExecutions);
    }

    [Fact]
    public async Task Given120TreasuryPayments_WhenProcessingAndRechecking_ThenRouteThreeCurrenciesAndKeepOneFinancialEffect()
    {
        // Given a representative batch using three purposes, three destinations and both banks.
        await using var fixture = new Fixture();
        await fixture.Initialize();
        var input = TreasuryData.Batch(fixture, 120, mixed: true);
        var receipt = await fixture.Accept(input);
        Assert.True((await fixture.Accept(input)).Replayed);
        // When each payment independently progresses, including a definitive failure and a lost bank response.
        for (var i = 0; i < 120; i++) Assert.True(await fixture.Processor.DispatchOne("treasury-worker", default));
        Assert.False(await fixture.Processor.DispatchOne("treasury-worker", default));
        var summary = await fixture.Store.Summary(null, null, default);
        // Then currencies never collapse into a mixed total and route evidence is durable.
        Assert.Equal(118, summary.StatusCounts["Accepted"]);
        Assert.Equal(1, summary.StatusCounts["Rejected"]);
        Assert.Equal(1, summary.StatusCounts["OutcomeUnknown"]);
        Assert.Equal(new[] { "EUR", "GBP", "USD" }, summary.Totals.Select(x => x.Currency));
        Assert.All(summary.Totals, x => Assert.Equal("40000", x.MinorUnits));
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var instructions = await db.PaymentInstructions.ToArrayAsync();
            Assert.Equal(3, instructions.Select(x => x.Purpose).Distinct().Count());
            Assert.Equal(3, instructions.Select(x => x.DestinationCountry).Distinct().Count());
            Assert.Equal(2, instructions.Select(x => x.Provider).Distinct().Count());
            Assert.All(instructions, x => Assert.Equal("mock-routing-v1", x.Treasury!.RoutingPolicyVersion));
            Assert.All(await db.PaymentAuditEvents.Where(x => x.Version == 0).ToArrayAsync(), x => Assert.Contains("routing=mock-routing-v1", x.Reason));
        }
        var investigation = Assert.Single((await fixture.Store.Cases(1, 20, null, default)).Items);
        await fixture.Reconciler.Recheck(investigation.Id, investigation.Version, Guid.NewGuid(), default);
        var detail = await fixture.Store.Payment(investigation.PaymentId, default);
        Assert.Equal("Accepted", detail.Payment.Status);
        Assert.Equal("bank-a", detail.Payment.Provider);
        Assert.All(detail.Attempts, x => Assert.Equal(detail.Payment.Treasury!.FundingAccountId, x.FundingAccountId));
        Assert.Equal("SubmissionCompletedWithExceptions", (await fixture.Store.Batch(receipt.Id, default)).Status);
        await using var check = fixture.Factory.CreateDbContext();
        Assert.Equal(119, await check.SimulatedBankExecutions.SumAsync(x => x.FinancialExecutions));
        Assert.Equal(120, await check.PaymentAttempts.CountAsync(x => x.Kind == "Submit"));
        Assert.True((await fixture.Accept(input)).Replayed);
    }

    [Fact]
    public async Task GivenFutureTreasuryDate_WhenWorkerRuns_ThenNoEarlyAttemptAndExecutionStartsWhenUtcDateIsDue()
    {
        await using var fixture = new Fixture();
        await fixture.Initialize();
        var input = TreasuryData.Batch(fixture, 1);
        input = input with { Payments = [input.Payments[0] with { ExecutionDate = input.Payments[0].ExecutionDate.AddDays(1) }] };
        await fixture.Accept(input);
        Assert.False(await fixture.Processor.DispatchOne("early", default));
        await using (var db = fixture.Factory.CreateDbContext())
        {
            Assert.Empty(await db.PaymentAttempts.ToArrayAsync());
            Assert.Empty(await db.SimulatedBankExecutions.ToArrayAsync());
        }
        fixture.Clock.Now = fixture.Clock.Now.AddDays(1);
        Assert.True(await fixture.Processor.DispatchOne("due", default));
        await using var check = fixture.Factory.CreateDbContext();
        Assert.Equal(PaymentStatus.Accepted, (await check.PaymentExecutions.SingleAsync()).Status);
        Assert.Equal(1, (await check.SimulatedBankExecutions.SingleAsync()).FinancialExecutions);
    }

    [Fact]
    public async Task GivenBankBExecutedButResultCommitFailed_WhenRestarting_ThenReconcileOriginalUsdRouteWithoutResubmission()
    {
        await using var fixture = new Fixture();
        await fixture.Initialize();
        var batch = TreasuryData.Batch(fixture, 3);
        await fixture.Accept(batch with { Payments = [batch.Payments[2]] });
        fixture.Fault.Operation = "complete";
        await Assert.ThrowsAsync<IOException>(() => fixture.Processor.DispatchOne("original", default));
        fixture.Clock.Now = fixture.Clock.Now.AddMinutes(1);
        var restarted = new DispatchService(new PaymentStoreHarness(fixture.Factory, fixture.Fault, fixture.Clock), new TestBank(fixture.Bank), fixture.Clock);
        Assert.Equal(1, await restarted.Recover(default));
        Assert.False(await restarted.DispatchOne("restart", default));
        var investigation = Assert.Single((await fixture.Store.Cases(1, 20, null, default)).Items);
        await fixture.Reconciler.Recheck(investigation.Id, investigation.Version, Guid.NewGuid(), default);
        var detail = await fixture.Store.Payment(investigation.PaymentId, default);
        Assert.Equal("bank-b", detail.Payment.Provider);
        Assert.Equal("USD", detail.Payment.Currency);
        Assert.Equal("Accepted", detail.Payment.Status);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal(1, (await db.SimulatedBankExecutions.SingleAsync()).FinancialExecutions);
        Assert.Equal(1, await db.PaymentAttempts.CountAsync(x => x.Kind == "Submit"));
        // A reroute cannot mutate an already accepted instruction or reuse its key at another bank.
        var instruction = await db.PaymentInstructions.SingleAsync();
        db.Entry(instruction).Property(x => x.Provider).CurrentValue = "bank-a";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        var work = new WorkItem(instruction, await db.PaymentExecutions.SingleAsync(), Guid.NewGuid(), "tampered", "corr", null, 2);
        await Assert.ThrowsAsync<ConflictException>(() => fixture.Bank.Submit("bank-a", MockSubmission.From(work), default));
    }

    [Fact]
    public async Task GivenEnhancedConcurrentClaims_WhenCompeting_ThenOneLeaseAndImmutableTreasurySnapshot()
    {
        await using var fixture = new Fixture();
        await fixture.Initialize();
        await fixture.Accept(TreasuryData.Batch(fixture, 1));
        var claims = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(() =>
            fixture.Store.Claim($"worker-{i}", fixture.Clock.Now.UtcDateTime, TimeSpan.FromSeconds(30), default))));
        Assert.Single(claims, x => x is not null);
        await using var db = fixture.Factory.CreateDbContext();
        var instruction = await db.PaymentInstructions.SingleAsync();
        db.Entry(instruction.Treasury!).Property(x => x.FundingAccountId).CurrentValue = "funding-company-uk-eur-a";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}
