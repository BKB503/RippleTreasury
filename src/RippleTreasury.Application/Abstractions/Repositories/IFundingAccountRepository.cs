using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Read-only tenant-scoped corporate funding reference data.</summary>
public interface IFundingAccountRepository
{
    /// <summary>Loads a source account only within the submitting tenant.</summary>
    Task<FundingAccount?> Find(string id, string tenant, CancellationToken ct);
    /// <summary>Lists active source accounts for the tenant without exposing credentials or balances.</summary>
    Task<FundingAccount[]> List(string tenant, CancellationToken ct);
}
