using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

/// <summary>Tenant-scoped reference-data reads; contains no liquidity or account-balance logic.</summary>
public class FundingAccountRepository(IDbContextFactory<TreasuryDb> factory) : IFundingAccountRepository
{
    /// <summary>Reads the specified account only if it belongs to the current submitting tenant.</summary>
    public async Task<FundingAccount?> Find(string id, string tenant, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.FundingAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.TenantId == tenant, ct);
    }

    /// <summary>Lists active account metadata for generation/selection without inventing available funds.</summary>
    public async Task<FundingAccount[]> List(string tenant, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.FundingAccounts.AsNoTracking().Where(x => x.TenantId == tenant && x.IsActive).OrderBy(x => x.Id).ToArrayAsync(ct);
    }
}
