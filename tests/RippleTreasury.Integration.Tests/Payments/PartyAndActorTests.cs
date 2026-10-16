using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public class PartyAndActorTests
{
    [Fact]

    public async Task SnapshotsAndActorAreAtomicImmutableAndPartOfIdempotency()
    {
        await using var f = new Fixture();
        await f.Initialize();
        var actor = new MutableActor();
        var store = new BatchIntakeRepository(f.Factory, f.Clock, f.Fault, actor);
        var service = new BatchService(store);
        var input = f.Input("accept", "reject", "accept");
        f.Fault.Operation = "accept";
        await Assert.ThrowsAsync<IOException>(() => service.Accept(input, "demo-tenant", "party-key", "corr", null, default));
        await using (var db = f.Factory.CreateDbContext())
        {
            Assert.Empty(await db.PaymentPartySnapshots.ToArrayAsync());
            Assert.Empty(await db.PaymentAuditEvents.ToArrayAsync());
        }
        var receipt = await service.Accept(input, "demo-tenant", "party-key", "corr", null, default);
        actor.Current = new("ben", "Ben Patel", "DemoUser");
        Assert.True((await service.Accept(input, "demo-tenant", "party-key", "corr", null, default)).Replayed);
        var original = input.Payments[0];
        var parties = original.Parties!;
        await Assert.ThrowsAsync<ConflictException>(() => service.Accept(input with { Payments = [original with { Parties = parties with { BusinessReference = "new-invoice" } }] }, "demo-tenant", "party-key", "corr", null, default));
        await using var check = f.Factory.CreateDbContext();
        Assert.Equal(6, await check.PaymentPartySnapshots.CountAsync());
        Assert.All(await check.PaymentAuditEvents.ToArrayAsync(), x => { Assert.Equal("alice", x.Actor); Assert.Equal("Alice Morgan", x.ActorDisplayName); Assert.Equal("DemoUser", x.ActorKind); });
        var row = await check.PaymentPartySnapshots.FirstAsync();
        check.Entry(row).Property(x => x.LegalName).CurrentValue = "Changed";
        await Assert.ThrowsAsync<InvalidOperationException>(() => check.SaveChangesAsync());
        Assert.Throws<InvalidOperationException>(() => check.SaveChanges());
        check.ChangeTracker.Clear();
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => check.Database.ExecuteSqlRawAsync("UPDATE PaymentPartySnapshots SET LegalName = 'Changed'"));
        var view = await f.Store.Batch(receipt.Id, default);
        Assert.Equal(3, view.PaymentCount);
    }

    [Fact]

    public async Task RecheckFinishesWithDurableInitiatingActorEvenWhenRequestContextChanges()
    {
        await using var f = new Fixture();
        await f.Initialize();
        await f.Accept(f.Input("lost-response"));
        f.Clock.Now = f.Clock.Now.AddMinutes(1);
        await f.Processor.DispatchOne("worker-1", default);
        var cases = await f.Store.Cases(1, 20, null, default);
        var c = Assert.Single(cases.Items);
        var actors = new MutableActor { Current = new("ben", "Ben Patel", "DemoUser") };
        var store = new PaymentExecutionRepository(f.Factory, f.Fault, actors);
        f.Clock.Now = f.Clock.Now.AddMinutes(1);
        var work = await store.BeginRecheck(c.Id, c.Version, Guid.NewGuid(), f.Clock.Now.UtcDateTime, default);
        actors.Current = new("cara", "Cara Wilson", "DemoUser");
        f.Clock.Now = f.Clock.Now.AddSeconds(5);
        await store.FinishRecheck(work, await f.Bank.Lookup(work.Instruction.Provider, work.Execution.ExecutionId, default), f.Clock.Now.UtcDateTime, default);
        var detail = await f.Store.Payment(work.Instruction.Id, default);
        Assert.All(detail.Audit.Where(x => x.Source == "Reconciliation"), x => { Assert.Equal("ben", x.Actor); Assert.Equal("Ben Patel", x.ActorDisplayName); });
        Assert.Contains(detail.Audit, x => x.Actor == "worker-1" && x.ActorKind == "System");
        Assert.NotNull(detail.History);
        Assert.Equal(DateTimeKind.Utc, detail.History.CreatedAt.Kind);
        Assert.True(detail.History.LastUpdatedAt > detail.History.CreatedAt);
        Assert.True(detail.History.LastReconciliationCompletedAt > detail.History.LastReconciliationStartedAt);
        Assert.Equal("ben", detail.History.LastReconciliationBy!.Id);
        Assert.Equal("ReconciliationCompleted", detail.Audit.Last().Action);
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(1, (await db.SimulatedBankExecutions.SingleAsync()).FinancialExecutions);
    }

    [Fact]

    public async Task HttpControllersKeepRequestActorsIsolatedAndValidateBinding()
    {
        await using var f = new Fixture();
        using var host = new ApiFactory(f.Path);
        async Task<(PaymentBatchReceiptResponseModel Receipt, string User)> Submit(string user)
        {
            using var client = host.CreateClient();
            client.DefaultRequestHeaders.Add("X-Demo-User", user);
            client.DefaultRequestHeaders.Add("Idempotency-Key", user);
            var input = f.Input("accept") with
            {
                ClientReference = user
            };
            var response = await client.PostAsJsonAsync("/api/v1/payment-batches", input);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            return ((await response.Content.ReadFromJsonAsync<PaymentBatchReceiptResponseModel>())!, user);
        }
        var requests = await Task.WhenAll(Submit("alice.treasury"), Submit("cara.finance"));
        using var reader = host.CreateClient();
        foreach (var request in requests)
        {
            var payments = await reader.GetFromJsonAsync<PagedResponseModel<PaymentResponseModel>>($"/api/v1/payment-batches/{request.Receipt.Id}/payments");
            var payment = Assert.Single(payments!.Items);
            Assert.NotNull(payment.Parties);
            Assert.Equal("Northstar Treasury UK Ltd", payment.Parties.Payer.LegalName);
            var detail = await reader.GetFromJsonAsync<PaymentDetailResponseModel>($"/api/v1/payments/{payment.Id}");
            Assert.Equal(request.User, Assert.Single(detail!.Audit).Actor);
        }
        var missingHeader = await reader.PostAsJsonAsync("/api/v1/payment-batches", f.Input("accept"));
        Assert.Equal(HttpStatusCode.BadRequest, missingHeader.StatusCode);
        Assert.Contains("correlationId", await missingHeader.Content.ReadAsStringAsync());
        reader.DefaultRequestHeaders.Add("X-Demo-User", "forged-user");
        reader.DefaultRequestHeaders.Add("Idempotency-Key", "forged");
        var forged = await reader.PostAsJsonAsync("/api/v1/payment-batches", f.Input("accept") with
        {
            ClientReference = "forged"
        });
        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        Assert.Contains("X-Demo-User", await forged.Content.ReadAsStringAsync());
    }
}
