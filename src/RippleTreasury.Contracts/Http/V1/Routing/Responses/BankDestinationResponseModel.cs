namespace RippleTreasury.Contracts;

/// <summary>One supported simulated-bank payout country/currency pair.</summary>
public record BankDestinationResponseModel(
    string Country,
    string Currency);
