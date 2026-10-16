using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public sealed class Fixture : IAsyncDisposable
{

    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ripple-{Guid.NewGuid():N}.db");
    public TestClock Clock { get; } = new(); public TestFault Fault { get; } = new();
    public TestFactory Factory { get; }
    public PaymentStoreHarness Store { get; }
    public MockBankEngine Bank { get; }
    public DispatchService Processor { get; }
    public ReconciliationService Reconciler { get; }
    public IBatchService Intake { get; }

    public Fixture()
    {
        Factory = new(new DbContextOptionsBuilder<TreasuryDb>().UseSqlite($"Data Source={Path};Default Timeout=10;Foreign Keys=True").Options);
        Store = new(Factory, Fault, Clock);
        Bank = new(Factory);
        Processor = new(Store, new TestBank(Bank), Clock);
        Intake = new BatchService(Store, new TreasuryInstructionFactory(new FundingAccountRepository(Factory),
            new BankRoutingPolicy(new DemoBankCapabilityCatalog())));
        Reconciler = new(Store, new TestBank(Bank), Clock);
    }

    public async Task Initialize()
    {
        await using var db = Factory.CreateDbContext();
        await TreasuryDatabaseInitializer.InitializeAsync(db);
    }

    public CreatePaymentBatchRequestModel Input(params string[] scenarios) => new("batch", scenarios.Select((s, i) => PartyData.Enrich(new PaymentInstructionRequestModel($"p-{i}", 10m, i % 2 == 0 ? "GBP" : "EUR", "Mock beneficiary", i % 2 == 0 ? "GB" : "DE", i % 2 == 0 ? "bank-a" : "bank-b", Enum.GetValues<PaymentPurpose>()[i % 3].ToString(), DateOnly.FromDateTime(Clock.Now.UtcDateTime), s))).ToArray());
    public Task<PaymentBatchReceiptResponseModel> Accept(CreatePaymentBatchRequestModel input, string key = "key") => Intake.Accept(input, "demo-tenant", key, "test-correlation", null, default);

    public async ValueTask DisposeAsync()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
            if (File.Exists(Path + suffix))
                File.Delete(Path + suffix);
        await Task.CompletedTask;
    }
}
