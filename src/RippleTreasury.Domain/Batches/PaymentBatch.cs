namespace RippleTreasury.Domain;

/// <summary>Accepted batch identity and submitting-organisation context; membership is immutable.</summary>
public class PaymentBatch
{
    public Guid Id { get; init; }
    public string Tenant { get; init; } = "";
    public string ClientReference { get; init; } = "";
    public string CorrelationId { get; init; } = "";
    public string? TraceParent { get; init; }
    public DateTime ReceivedAt { get; init; }
}
