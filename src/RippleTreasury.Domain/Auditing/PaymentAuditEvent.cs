namespace RippleTreasury.Domain;

/// <summary>Append-only business evidence, committed in the same transaction as its state transition.</summary>
public class PaymentAuditEvent
{
    public Guid Id { get; init; }
    public string Action { get; init; } = "";
    public string ActorDisplayName { get; init; } = "";
    public string ActorKind { get; init; } = "System";
    public string Source { get; init; } = "";
    public Guid PaymentId { get; init; }
    public Guid ExecutionId { get; init; }
    public Guid BatchId { get; init; }
    public DateTime At { get; init; }
    public string Before { get; init; } = "";
    public string After { get; init; } = "";
    public int Version { get; init; }
    public string Actor { get; init; } = "";
    public string Reason { get; init; } = "";
    public string CorrelationId { get; init; } = "";
    public Guid? AttemptId { get; init; }
    public string PolicyVersion { get; init; } = PaymentPolicy.Version;
}
