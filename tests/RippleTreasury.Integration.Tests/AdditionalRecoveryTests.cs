using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Integration.Tests;

public class AdditionalRecoveryTests
{
    [Fact]

    public async Task InterruptedLookupRecoversAuditedCaseAndRecheckRemainsSafe()
    {
        await using var f = new Fixture();
        await f.Initialize();
        await f.Accept(f.Input("lost-response"));
        await f.Processor.DispatchOne("w", default);
        var c = Assert.Single((await f.Store.Cases(1, 20, null, default)).Items);
        await f.Store.BeginRecheck(c.Id, c.Version, Guid.NewGuid(), f.Clock.Now.UtcDateTime, default);
        f.Clock.Now = f.Clock.Now.AddMinutes(1);
        await f.Processor.Recover(default);
        var recovered = await f.Store.Case(c.Id, default);
        Assert.Equal("AwaitingEvidence", recovered.Case.Status);
        Assert.Contains(recovered.Detail.Audit, a => a.Reason.Contains("Expired provider lookup lease"));
        await f.Reconciler.Recheck(c.Id, recovered.Case.Version, Guid.NewGuid(), default);
        Assert.Equal("Accepted", (await f.Store.Case(c.Id, default)).Detail.Payment.Status);
    }
    [Fact]

    public async Task FutureDateIsNotClaimedAndEquivalentAmountReplays()
    {
        await using var f = new Fixture();
        await f.Initialize();
        var input = f.Input("accept");
        input = input with
        {
            Payments = [input.Payments[0] with { ExecutionDate = DateOnly.FromDateTime(f.Clock.Now.UtcDateTime).AddDays(1) }]
        };
        var accepted = await f.Accept(input);
        var replay = await f.Accept(input with
        {
            Payments = [input.Payments[0] with { Amount = 10.00m }]
        });
        Assert.True(replay.Replayed);
        Assert.Equal(accepted.Id, replay.Id);
        Assert.False(await f.Processor.DispatchOne("w", default));
        f.Clock.Now = f.Clock.Now.AddDays(1);
        Assert.True(await f.Processor.DispatchOne("w", default));
    }
}
