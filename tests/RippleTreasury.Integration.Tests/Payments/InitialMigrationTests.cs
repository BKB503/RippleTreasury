using Microsoft.EntityFrameworkCore;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public class InitialMigrationTests
{
    [Fact]
    public async Task GivenEmptyDatabase_WhenInitialized_ThenBaselineAndTreasuryExtensionExist()
    {
        // Given a new disposable SQLite database.
        await using var fixture = new Fixture();

        // When the checked-in initial migration is applied.
        await fixture.Initialize();

        // Then the full current schema has the baseline plus its forward extension and reference tenant.
        await using var db = fixture.Factory.CreateDbContext();
        var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(2, migrations.Length);
        Assert.EndsWith("_InitialTreasury", migrations[0]);
        Assert.EndsWith("_AddTreasuryFundingAndRouting", migrations[1]);
        Assert.Equal(12, await db.FundingAccounts.CountAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal("Northstar Treasury Group", (await db.Tenants.SingleAsync()).Name);
        Assert.Empty(await db.PaymentBatches.ToArrayAsync());
    }

    [Fact]
    public async Task GivenAcceptedPayment_WhenInitializedAgain_ThenDataReplayAndGuardsRemain()
    {
        // Given accepted instructions with an append-only audit and idempotency record.
        await using var fixture = new Fixture();
        await fixture.Initialize();
        var input = fixture.Input("accept");
        var receipt = await fixture.Accept(input);

        // When startup initialization runs again against the same database.
        await fixture.Initialize();

        // Then initialization preserves the original payment and replay identity.
        var replay = await fixture.Accept(input);
        Assert.True(replay.Replayed);
        Assert.Equal(receipt.Id, replay.Id);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Single(await db.PaymentInstructions.ToArrayAsync());
        Assert.Single(await db.PaymentAuditEvents.ToArrayAsync());
        Assert.Equal(2, await db.PaymentPartySnapshots.CountAsync());
        Assert.Equal(2, (await db.Database.GetAppliedMigrationsAsync()).Count());
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
            db.Database.ExecuteSqlRawAsync("UPDATE PaymentInstructions SET AmountInMinorUnits = 1"));
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
            db.Database.ExecuteSqlRawAsync("DELETE FROM PaymentAuditEvents"));
    }
}