using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public class MockBankContractTests
{
    [Fact]

    public async Task StableBankKeyRequiresSameFinancialInstructionAndExecutesOnce()
    {
        await using var fixture = new Fixture();
        await fixture.Initialize();
        await fixture.Accept(fixture.Input("accept"));
        var item = (await fixture.Store.Claim("worker", fixture.Clock.Now.UtcDateTime, TimeSpan.FromSeconds(30), default))!;
        var request = MockSubmission.From(item);
        await fixture.Bank.Submit(item.Instruction.Provider, request, default);
        await fixture.Bank.Submit(item.Instruction.Provider, request with
        {
            AttemptNumber = 2
        }, default);
        await Assert.ThrowsAsync<ConflictException>(() => fixture.Bank.Submit(item.Instruction.Provider, request with { MinorUnits = request.MinorUnits + 1 }, default));
        await using var db = fixture.Factory.CreateDbContext();
        var executed = await db.SimulatedBankExecutions.SingleAsync();
        Assert.Equal(1, executed.FinancialExecutions);
        Assert.Equal(item.Instruction.Amount.MinorUnits, executed.MinorUnits);
        Assert.Equal(item.Instruction.Amount.Currency, executed.Currency);
    }
}
