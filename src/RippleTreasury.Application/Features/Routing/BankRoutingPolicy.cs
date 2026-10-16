using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Deterministic capability-based routing; financial account/corridor eligibility stays in Domain.</summary>
public class BankRoutingPolicy(IBankCapabilityCatalog catalog, ILogger<BankRoutingPolicy>? logger = null) : IBankRoutingPolicy
{
    public const string Version = "mock-routing-v1";

    /// <summary>Chooses configured priority then provider ID, or validates the requested bank without silent fallback.</summary>
    public BankRoutingDecision Select(FundingAccount funding, PaymentCorridor corridor, string? preferredBank)
    {
        var eligible = catalog.GetCapabilities().Where(x => x.Supports(funding, corridor))
            .OrderBy(x => x.Priority).ThenBy(x => x.Provider, StringComparer.Ordinal).ToArray();
        var chosen = string.IsNullOrWhiteSpace(preferredBank) ? eligible.FirstOrDefault() :
            eligible.SingleOrDefault(x => x.Provider == preferredBank);
        if (chosen is null)
        {
            (logger ?? NullLogger<BankRoutingPolicy>.Instance).LogWarning("Bank routing rejected with {ReasonCode}; eligible banks {EligibleCount}", "routing_unsupported", eligible.Length);
            throw new TreasuryValidationException("routing_unsupported", "No available bank supports the requested funding account, destination/currency and bank preference.");
        }
        (logger ?? NullLogger<BankRoutingPolicy>.Instance).LogDebug("Bank routing selected {Provider} under {RoutingPolicyVersion}", chosen.Provider, Version);
        var reason = $"{(string.IsNullOrWhiteSpace(preferredBank) ? "Configured priority" : "Validated preference")}; {corridor.SourceCountry}->{corridor.DestinationCountry}/{corridor.Currency}; {chosen.Provider}";
        return new(chosen.Provider, Version, reason);
    }
}
