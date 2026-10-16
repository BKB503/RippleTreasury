using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Provider-specific fixture/configuration boundary; not a live availability or pricing service.</summary>
public interface IBankCapabilityCatalog
{
    /// <summary>Returns configured provider availability, funding-account eligibility and supported corridors.</summary>
    IReadOnlyList<BankCapability> GetCapabilities();
}
