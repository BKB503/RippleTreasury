using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public class TreasuryMigrationTests
{
    [Fact]
    public async Task GivenPopulatedVersionOneDatabase_WhenApplyingForwardMigration_ThenPreserveRowsHashesAuditAndBankExecution()
    {
        // Given the original checked-in baseline, with real persisted instructions and hashes.
        await using var fixture = new Fixture();
        var batchId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var auditId = Guid.NewGuid();
        var timestamp = fixture.Clock.Now.UtcDateTime;
        var date = DateOnly.FromDateTime(timestamp);
        var line = new PaymentInstructionRequestModel("p-1", 12.50m, "GBP", "Vendor", "GB", "bank-a", "VendorDisbursement", date);
        var input = new CreatePaymentBatchRequestModel("legacy", [line]);
        const string canonical = """{"ClientReference":"legacy","Payments":[{"ClientReference":"p-1","MinorUnits":1250,"Currency":"GBP","Beneficiary":"Vendor","DestinationCountry":"GB","Provider":"bank-a","Purpose":0,"ExecutionDate":"2026-10-14","Scenario":"accept"}]}""";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        var submission = new MockSubmission(executionId, "accept", 1, 1250, "GBP", "Vendor", "GB", "VendorDisbursement", date);
        var bankHash = submission.Fingerprint();
        await using (var old = fixture.Factory.CreateDbContext())
        {
            var initial = old.Database.GetMigrations().Single(x => x.EndsWith("_InitialTreasury"));
            await old.GetService<IMigrator>().MigrateAsync(initial);
            await old.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PaymentBatches (Id,TenantId,ClientReference,CorrelationId,ReceivedAt) VALUES ({batchId},{"demo-tenant"},{"legacy"},{"historic-correlation"},{timestamp})");
            await old.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PaymentInstructions (Id,BatchId,ClientReference,AmountInMinorUnits,CurrencyCode,Beneficiary,DestinationCountry,BankProviderCode,PaymentPurpose,RequestedExecutionDate,Scenario) VALUES ({paymentId},{batchId},{"p-1"},{1250},{"GBP"},{"Vendor"},{"GB"},{"bank-a"},{"VendorDisbursement"},{date},{"accept"})");
            await old.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PaymentExecutions (Id,ExecutionId,Status,Version,SubmissionCount) VALUES ({paymentId},{executionId},{0},{0},{0})");
            await old.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO IdempotencyRequests (Id,TenantId,Key,Hash,BatchId) VALUES ({Guid.NewGuid()},{"demo-tenant"},{"key"},{hash},{batchId})");
            await old.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PaymentAuditEvents (Id,Action,ActorDisplayName,ActorKind,Source,PaymentId,ExecutionId,BatchId,At,Before,After,Version,Actor,Reason,CorrelationId,PolicyVersion) VALUES ({auditId},{"InstructionCreated"},{"Alice Morgan"},{"DemoUser"},{"BatchIntake"},{paymentId},{executionId},{batchId},{timestamp},{"Ready"},{"Ready"},{0},{"alice.treasury"},{"Instruction accepted"},{"historic-correlation"},{"mock-v1"})");
            await old.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO MockBankExecutions (Id,Provider,Outcome,Evidence,FinancialExecutions,InstructionHash,AmountInMinorUnits,CurrencyCode) VALUES ({executionId},{"bank-a"},{"Accepted"},{"historic-bank-reference"},{1},{bankHash},{1250},{"GBP"})");
            await old.Database.ExecuteSqlRawAsync("CREATE TRIGGER instruction_no_update BEFORE UPDATE ON PaymentInstructions BEGIN SELECT RAISE(ABORT, 'immutable instruction'); END;");
            await old.Database.ExecuteSqlRawAsync("CREATE TRIGGER audit_no_update BEFORE UPDATE ON PaymentAuditEvents BEGIN SELECT RAISE(ABORT, 'append-only audit'); END;");
        }
        // When the forward extension is applied without deleting or rewriting the original database.
        await fixture.Initialize();
        // Then original request replay and provider execution replay retain their exact identities and evidence.
        var replay = await fixture.Accept(input);
        Assert.True(replay.Replayed);
        Assert.Equal(batchId, replay.Id);
        Assert.Equal("historic-bank-reference", (await fixture.Bank.Submit("bank-a", submission, default)).Evidence);
        await using var check = fixture.Factory.CreateDbContext();
        Assert.Equal(hash, (await check.IdempotencyRequests.SingleAsync()).Hash);
        Assert.Null((await check.PaymentInstructions.SingleAsync()).Treasury);
        var audit = await check.PaymentAuditEvents.SingleAsync();
        Assert.Equal(auditId, audit.Id);
        Assert.Equal("Alice Morgan", audit.ActorDisplayName);
        Assert.Equal("historic-correlation", audit.CorrelationId);
        Assert.Equal(bankHash, (await check.SimulatedBankExecutions.SingleAsync()).InstructionHash);
        Assert.Equal(1, (await check.SimulatedBankExecutions.SingleAsync()).FinancialExecutions);
        Assert.Equal(12, await check.FundingAccounts.CountAsync());
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => check.Database.ExecuteSqlRawAsync("UPDATE PaymentInstructions SET AmountInMinorUnits = 1"));
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => check.Database.ExecuteSqlRawAsync("DELETE FROM PaymentAuditEvents"));
    }
}
