using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RippleTreasury.Infrastructure;

public sealed class DesignFactory : IDesignTimeDbContextFactory<TreasuryDb>
{

    /// <summary>Supplies the EF CLI design-time SQLite context; runtime hosts use configured database paths.</summary>
    public TreasuryDb CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<TreasuryDb>().UseSqlite("Data Source=treasury.db;Foreign Keys=True").Options);
}
