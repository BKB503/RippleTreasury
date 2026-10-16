using RippleTreasury.Contracts;

namespace RippleTreasury.Application;

/// <summary>Read-only funding reference-data use case.</summary>
public interface IFundingAccountQueries
{
    /// <summary>Returns active source-account choices within the submitting tenant.</summary>
    Task<FundingAccountResponseModel[]> List(string tenant, CancellationToken ct);
}
