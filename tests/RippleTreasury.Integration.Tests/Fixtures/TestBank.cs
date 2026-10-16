using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public sealed class TestBank(MockBankEngine engine) : IBankGateway
{

    public Task<BankObservation> Submit(WorkItem item, CancellationToken ct) => engine.Submit(item.Instruction.Provider, MockSubmission.From(item), ct);
    public Task<BankObservation> Lookup(WorkItem item, CancellationToken ct) => engine.Lookup(item.Instruction.Provider, item.Execution.ExecutionId, ct);
}
