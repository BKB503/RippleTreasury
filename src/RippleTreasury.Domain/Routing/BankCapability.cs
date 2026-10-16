namespace RippleTreasury.Domain;

/// <summary>Configured mock-bank eligibility, including funding accounts rather than currency-only selection.</summary>
public sealed record BankCapability(string Provider, int Priority, bool IsAvailable,
    IReadOnlyList<string> FundingAccountIds, IReadOnlyList<PaymentDestination> Destinations)
{
    /// <summary>Tests availability, funding-bank ownership and the supported destination/currency together.</summary>
    public bool Supports(FundingAccount account, PaymentCorridor corridor) => IsAvailable && account.IsActive &&
        account.BankProviderCode == Provider && account.Country == corridor.SourceCountry &&
        account.CurrencyCode == corridor.Currency && FundingAccountIds.Contains(account.Id) &&
        Destinations.Any(x => x.Country == corridor.DestinationCountry && x.Currency == corridor.Currency);
}
