namespace RippleTreasury.Domain;

/// <summary>A destination country and payout currency supported by a simulated provider.</summary>
public sealed record PaymentDestination(string Country, string Currency);
