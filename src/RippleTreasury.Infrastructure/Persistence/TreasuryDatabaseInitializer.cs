using Microsoft.EntityFrameworkCore;

namespace RippleTreasury.Infrastructure;

/// <summary>Applies local EF migrations, enables SQLite WAL and installs immutable-data guards.</summary>
/// <remarks>Production deployment must run reviewed migrations in a controlled release job.</remarks>
public static class TreasuryDatabaseInitializer
{

    /// <summary>Applies forward migrations, enables SQLite WAL and reinstalls idempotent database immutability guards.</summary>
    public static async Task InitializeAsync(TreasuryDb db, CancellationToken ct = default)
    {
        // Repeated startup preserves existing data; it never drops or recreates the database.
        await db.Database.MigrateAsync(ct);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER IF NOT EXISTS audit_no_update BEFORE UPDATE ON PaymentAuditEvents BEGIN SELECT RAISE(ABORT, 'append-only audit'); END;", ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER IF NOT EXISTS audit_no_delete BEFORE DELETE ON PaymentAuditEvents BEGIN SELECT RAISE(ABORT, 'append-only audit'); END;", ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER IF NOT EXISTS instruction_no_update BEFORE UPDATE ON PaymentInstructions BEGIN SELECT RAISE(ABORT, 'immutable instruction'); END;", ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER IF NOT EXISTS party_no_update BEFORE UPDATE ON PaymentPartySnapshots BEGIN SELECT RAISE(ABORT, 'immutable party snapshot'); END;", ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER IF NOT EXISTS party_no_delete BEFORE DELETE ON PaymentPartySnapshots BEGIN SELECT RAISE(ABORT, 'immutable party snapshot'); END;", ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER IF NOT EXISTS instruction_no_delete BEFORE DELETE ON PaymentInstructions BEGIN SELECT RAISE(ABORT, 'immutable instruction'); END;", ct);
    }
}
