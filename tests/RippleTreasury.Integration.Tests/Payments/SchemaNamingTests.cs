using RippleTreasury.Domain;
using Microsoft.EntityFrameworkCore;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public class SchemaNamingTests
{
    [Fact]
    public async Task PhysicalSchemaStoresExactAmountAndRejectsUnknownTenant()
    {
        await using var fixture = new Fixture();
        await fixture.Initialize();
        var input = fixture.Input("accept");
        input = input with { Payments = [input.Payments[0] with { Amount = 125.50m }] };
        await fixture.Accept(input);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal("Northstar Treasury Group", (await db.Tenants.SingleAsync()).Name);
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT AmountInMinorUnits, CurrencyCode, PaymentPurpose, BankProviderCode FROM PaymentInstructions";
        await using (var reader = await command.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            Assert.Equal(12550, reader.GetInt64(0));
            Assert.Equal("GBP", reader.GetString(1));
            Assert.Equal("VendorDisbursement", reader.GetString(2));
            Assert.Equal("bank-a", reader.GetString(3));
        }
        db.PaymentBatches.Add(new PaymentBatch { Id = Guid.NewGuid(), Tenant = "unknown-tenant", ClientReference = "new", CorrelationId = "test", ReceivedAt = DateTime.UtcNow });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
