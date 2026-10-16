namespace RippleTreasury.Domain;

/// <summary>Immutable provider selection evidence saved before any potentially submitted attempt.</summary>
public sealed record BankRoutingDecision(string Provider, string PolicyVersion, string Reason);
