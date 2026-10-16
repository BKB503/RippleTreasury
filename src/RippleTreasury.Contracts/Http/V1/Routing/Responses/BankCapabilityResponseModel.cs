namespace RippleTreasury.Contracts;

/// <summary>Configured mock provider priority, availability, eligible source-account IDs and payout corridors.</summary>
public record BankCapabilityResponseModel(
    string Provider,
    int Priority,
    bool IsAvailable,
    string[] FundingAccountIds,
    BankDestinationResponseModel[] Destinations);
