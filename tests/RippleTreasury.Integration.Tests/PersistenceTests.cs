using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public class PersistenceTests
{
    [Fact]

    public async Task ReplayConflictAndConcurrentIntake()
    {
        await using var f = new Fixture();
        await f.Initialize();
        var input = f.Input("accept", "reject");
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => f.Accept(input))));
        Assert.Single(results.Select(x => x.Id).Distinct());
        Assert.Equal(7, results.Count(x => x.Replayed));
        await Assert.ThrowsAsync<ConflictException>(() => f.Accept(input with { Payments = [input.Payments[0] with { Amount = 20m }] }));
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(1, await db.PaymentBatches.CountAsync());
        Assert.Equal(2, await db.PaymentInstructions.CountAsync());
        Assert.Equal(2, await db.PaymentAuditEvents.CountAsync());
    }
    [Fact]

    public async Task IntakeIsAtomicAndValidated()
    {
        await using var f = new Fixture();
        await f.Initialize();
        var input = f.Input("accept", "accept");
        await Assert.ThrowsAsync<InputException>(() => f.Accept(input with { Payments = [input.Payments[0], input.Payments[1] with { Amount = 0m }] }));
        f.Fault.Operation = "accept";
        await Assert.ThrowsAsync<IOException>(() => f.Accept(input));
        await using var db = f.Factory.CreateDbContext();
        Assert.Empty(await db.PaymentBatches.ToArrayAsync());
        Assert.Empty(await db.PaymentAuditEvents.ToArrayAsync());
        Assert.Empty(await db.IdempotencyRequests.ToArrayAsync());
        await f.Accept(input);
    }
    [Fact]

    public async Task ConcurrentClaimsHaveOneOwnerAndVersionPreventsLateWriter()
    {
        await using var f = new Fixture();
        await f.Initialize();
        await f.Accept(f.Input("accept"));
        var claims = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(() => f.Store.Claim($"w-{i}", f.Clock.Now.UtcDateTime, TimeSpan.FromSeconds(30), default))));
        var claim = Assert.Single(claims, x => x != null)!;
        f.Clock.Now = f.Clock.Now.AddMinutes(1);
        Assert.Equal(1, await f.Processor.Recover(default));
        await Assert.ThrowsAsync<VersionConflictException>(() => f.Store.Complete(claim, new(BankOutcome.Accepted, "late"), f.Clock.Now.UtcDateTime, default));
        Assert.Null(await f.Store.Claim("new-owner", f.Clock.Now.UtcDateTime, TimeSpan.FromSeconds(30), default));
    }
    [Fact]

    public async Task AuditFailureRollsBackOutcomeThenRestartReconcilesOneExecution()
    {
        await using var f = new Fixture();
        await f.Initialize();
        await f.Accept(f.Input("accept"));
        f.Fault.Operation = "complete";
        await Assert.ThrowsAsync<IOException>(() => f.Processor.DispatchOne("w", default));
        await using (var db = f.Factory.CreateDbContext())
        {
            Assert.Equal(PaymentStatus.Submitting, (await db.PaymentExecutions.SingleAsync()).Status);
            Assert.Equal(2, await db.PaymentAuditEvents.CountAsync());
            Assert.Equal(1, (await db.SimulatedBankExecutions.SingleAsync()).FinancialExecutions);
        }
        f.Clock.Now = f.Clock.Now.AddMinutes(1);
        var restarted = new DispatchService(new PaymentStoreHarness(f.Factory, f.Fault, f.Clock), new TestBank(f.Bank), f.Clock);
        Assert.Equal(1, await restarted.Recover(default));
        Assert.False(await restarted.DispatchOne("restart", default));
        var c = Assert.Single((await f.Store.Cases(1, 20, null, default)).Items);
        await new ReconciliationService(new PaymentStoreHarness(f.Factory, f.Fault, f.Clock), new TestBank(f.Bank), f.Clock).Recheck(c.Id, c.Version, Guid.NewGuid(), default);
        await using (var db = f.Factory.CreateDbContext())
        {
            Assert.Equal(PaymentStatus.Accepted, (await db.PaymentExecutions.SingleAsync()).Status);
            Assert.Equal(1, (await db.SimulatedBankExecutions.SingleAsync()).FinancialExecutions);
            Assert.Equal(1, await db.PaymentAttempts.CountAsync(x => x.Kind == "Submit"));
            Assert.Equal(5, await db.PaymentAuditEvents.CountAsync());
        }
    }
    [Fact]

    public async Task FiveHundredMixedPaymentsProgressIndependentlyAndLostResponseReconciles()
    {
        await using var f = new Fixture();
        await f.Initialize();
        var input = f.Input(Enumerable.Range(0, 500).Select(i => i == 0 ? "lost-response" : i % 10 == 0 ? "reject" : "accept").ToArray());
        var receipt = await f.Accept(input);
        for (var i = 0; i < 500; i++)
            Assert.True(await f.Processor.DispatchOne("worker", default));
        Assert.False(await f.Processor.DispatchOne("worker", default));
        var summary = await f.Store.Summary(null, null, default);
        Assert.Equal(500, summary.Total);
        Assert.Equal(450, summary.StatusCounts["Accepted"]);
        Assert.Equal(49, summary.StatusCounts["Rejected"]);
        Assert.Equal(1, summary.StatusCounts["OutcomeUnknown"]);
        Assert.All(summary.Totals, t => Assert.Equal("250000", t.MinorUnits));
        Assert.Equal("ActionRequired", (await f.Store.Batch(receipt.Id, default)).Status);
        var c = Assert.Single((await f.Store.Cases(1, 20, null, default)).Items);
        await f.Reconciler.Recheck(c.Id, c.Version, Guid.NewGuid(), default);
        Assert.Equal("SubmissionCompletedWithExceptions", (await f.Store.Batch(receipt.Id, default)).Status);
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(451, await db.SimulatedBankExecutions.SumAsync(x => x.FinancialExecutions));
        Assert.Equal(500, await db.PaymentAttempts.CountAsync(x => x.Kind == "Submit"));
        Assert.All(await db.PaymentAuditEvents.ToArrayAsync(), a => Assert.Equal("test-correlation", a.CorrelationId));
    }
    [Fact]

    public async Task CrashBeforeSendNeverAutomaticallyResendsWithoutEvidence()
    {
        await using var f = new Fixture();
        await f.Initialize();
        await f.Accept(f.Input("accept"));
        await f.Store.Claim("crashed", f.Clock.Now.UtcDateTime, TimeSpan.FromSeconds(30), default);
        f.Clock.Now = f.Clock.Now.AddMinutes(1);
        await f.Processor.Recover(default);
        var c = Assert.Single((await f.Store.Cases(1, 20, null, default)).Items);
        await f.Reconciler.Recheck(c.Id, c.Version, Guid.NewGuid(), default);
        Assert.Equal("ReconciliationRequired", (await f.Store.Case(c.Id, default)).Detail.Payment.Status);
        Assert.False(await f.Processor.DispatchOne("restart", default));
        await using var db = f.Factory.CreateDbContext();
        Assert.Empty(await db.SimulatedBankExecutions.ToArrayAsync());
    }
    [Fact]

    public async Task SafeTransientRetriesReuseExecutionIdentity()
    {
        await using var f = new Fixture();
        await f.Initialize();
        var receipt = await f.Accept(f.Input("safe-transient"));
        await f.Processor.DispatchOne("worker", default);
        Assert.False(await f.Processor.DispatchOne("worker", default));
        f.Clock.Now = f.Clock.Now.AddSeconds(3);
        Assert.True(await f.Processor.DispatchOne("worker", default));
        var p = Assert.Single((await f.Store.Payments(receipt.Id, 1, 20, default)).Items);
        Assert.Equal("Accepted", p.Status);
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(2, await db.PaymentAttempts.CountAsync());
        Assert.Equal(p.ExecutionId, (await db.SimulatedBankExecutions.SingleAsync()).Id);
        Assert.Equal(1, (await db.SimulatedBankExecutions.SingleAsync()).FinancialExecutions);
    }
    [Fact]

    public async Task StaleAndDuplicateRechecksAreBlocked()
    {
        await using var f = new Fixture();
        await f.Initialize();
        await f.Accept(f.Input("unknown"));
        await f.Processor.DispatchOne("w", default);
        var c = Assert.Single((await f.Store.Cases(1, 20, null, default)).Items);
        var command = Guid.NewGuid();
        await f.Reconciler.Recheck(c.Id, c.Version, command, default);
        await Assert.ThrowsAsync<VersionConflictException>(() => f.Reconciler.Recheck(c.Id, c.Version, Guid.NewGuid(), default));
        await Assert.ThrowsAsync<ConflictException>(() => f.Reconciler.Recheck(c.Id, c.Version, command, default));
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(1, await db.PaymentAttempts.CountAsync(x => x.Kind == "Lookup"));
    }
    [Fact]

    public async Task SqliteTriggersPreventAuditAndInstructionMutation()
    {
        await using var f = new Fixture();
        await f.Initialize();
        await f.Accept(f.Input("accept"));
        await using var db = f.Factory.CreateDbContext();
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM PaymentAuditEvents"));
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => db.Database.ExecuteSqlRawAsync("UPDATE PaymentInstructions SET AmountInMinorUnits=1"));
    }
    [Fact]

    public void ArchitectureHasNoInfrastructureReferencesInDomainOrApplication()
    {
        foreach (var assembly in new[] { typeof(Money).Assembly, typeof(BatchService).Assembly })
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), x => x.Name!.Contains("EntityFramework") || x.Name.Contains("Infrastructure") || x.Name.Contains("AspNetCore"));
    }
}
