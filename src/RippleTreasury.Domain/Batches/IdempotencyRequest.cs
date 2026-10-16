namespace RippleTreasury.Domain;

/// <summary>Retains the original accepted request hash and receipt for safe replay.</summary>
public class IdempotencyRequest
{
    public Guid Id { get; init; }
    public string Tenant { get; init; } = "";
    public string Key { get; init; } = "";
    public string Hash { get; init; } = "";
    public Guid BatchId { get; init; }
}
