using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

/// <summary>Maps the domain entities directly; provider configuration and persistence guards stay in Infrastructure.</summary>
public class TreasuryDb(DbContextOptions<TreasuryDb> options) : DbContext(options)
{

    public DbSet<PaymentPartySnapshot> PaymentPartySnapshots => Set<PaymentPartySnapshot>();
    public DbSet<PaymentBatch> PaymentBatches => Set<PaymentBatch>();
    public DbSet<PaymentInstruction> PaymentInstructions => Set<PaymentInstruction>();

    public DbSet<PaymentExecution> PaymentExecutions => Set<PaymentExecution>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<ReconciliationCase> ReconciliationCases => Set<ReconciliationCase>();
    public DbSet<PaymentAuditEvent> PaymentAuditEvents => Set<PaymentAuditEvent>();

    public DbSet<IdempotencyRequest> IdempotencyRequests => Set<IdempotencyRequest>();
    // Separate bank-simulator state proves that replay/recovery creates no second external execution.
    public DbSet<SimulatedBankExecution> SimulatedBankExecutions => Set<SimulatedBankExecution>();
    public DbSet<FundingAccount> FundingAccounts => Set<FundingAccount>();
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Applies external EF mappings and consistent UTC handling without moving domain rules into EF.</summary>
    protected override void OnModelCreating(ModelBuilder m)
    {
        m.ApplyConfigurationsFromAssembly(typeof(TreasuryDb).Assembly);

        var utc = new ValueConverter<DateTime, DateTime>(value => value.ToUniversalTime(), value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        foreach (var entity in m.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(utc);
    }

    /// <summary>Checks immutable financial records before committing synchronous changes.</summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureImmutable();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>Checks immutable financial records before committing asynchronous changes.</summary>
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        EnsureImmutable();
        return SaveChangesAsync(true, ct);
    }

    /// <summary>Checks immutable financial records before committing asynchronous changes.</summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        EnsureImmutable();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    /// <summary>Blocks changes/deletes to accepted intent, owned treasury/party snapshots and append-only audit events.</summary>
    private void EnsureImmutable()
    {
        if (ChangeTracker.Entries<TreasuryPaymentDetails>().Any(e => e.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<PaymentAuditEvent>().Any(e => e.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<PaymentInstruction>().Any(e => e.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<PaymentPartySnapshot>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit and accepted instructions are immutable.");
    }
}
