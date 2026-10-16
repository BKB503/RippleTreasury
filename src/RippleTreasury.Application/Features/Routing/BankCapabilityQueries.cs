using RippleTreasury.Contracts;

namespace RippleTreasury.Application;

/// <summary>Maps configured simulation capabilities to a public read-only response contract.</summary>
public class BankCapabilityQueries(IBankCapabilityCatalog catalog) : IBankCapabilityQueries
{
    /// <summary>Returns deterministic provider/account/corridor metadata for reviewer inspection.</summary>
    public BankCapabilityResponseModel[] List() => catalog.GetCapabilities().Select(x =>
        new BankCapabilityResponseModel(x.Provider, x.Priority, x.IsAvailable, x.FundingAccountIds.ToArray(),
            x.Destinations.Select(d => new BankDestinationResponseModel(d.Country, d.Currency)).ToArray())).ToArray();
}
