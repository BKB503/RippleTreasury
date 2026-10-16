using RippleTreasury.Contracts;

namespace RippleTreasury.Application;

/// <summary>Exposes safe corporate source-account choices without balances or sensitive account values.</summary>
public class FundingAccountQueries(IFundingAccountRepository repository) : IFundingAccountQueries
{
    /// <summary>Maps active tenant-scoped funding accounts to operation UI reference data.</summary>
    public async Task<FundingAccountResponseModel[]> List(string tenant, CancellationToken ct) =>
        (await repository.List(tenant, ct)).Select(x => new FundingAccountResponseModel(x.Id, x.OwnerReference,
            x.CurrencyCode, x.Country, x.BankProviderCode)).ToArray();
}
