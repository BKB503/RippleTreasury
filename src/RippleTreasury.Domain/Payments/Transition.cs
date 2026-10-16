namespace RippleTreasury.Domain;

public sealed record Transition(PaymentStatus Before, PaymentStatus After, int Version, string Reason);
