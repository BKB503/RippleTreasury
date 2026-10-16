using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

/// <summary>Deterministic simulation configuration, not real bank availability or rail certification.</summary>
public class DemoBankCapabilityCatalog : IBankCapabilityCatalog
{
    /// <summary>Returns banks with different account/corridor matrices and explicit stable priorities.</summary>
    public IReadOnlyList<BankCapability> GetCapabilities()
    {
        var accounts = DemoFundingAccounts.Create();
        return [
            new("bank-a", 10, true, accounts.Where(x => x.BankProviderCode == "bank-a").Select(x => x.Id).ToArray(),
                [new("GB", "GBP"), new("DE", "EUR")]),
            new("bank-b", 20, true, accounts.Where(x => x.BankProviderCode == "bank-b").Select(x => x.Id).ToArray(),
                [new("DE", "EUR"), new("US", "USD")])
        ];
    }
}
