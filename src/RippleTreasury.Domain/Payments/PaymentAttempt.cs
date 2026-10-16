namespace RippleTreasury.Domain;

/// <summary>Durable submission or lookup intent, created before contacting a bank.</summary>
public class PaymentAttempt
{
    public Guid Id { get; init; }
    public string ActorDisplayName { get; init; } = "";
    public string ActorKind { get; init; } = "System";
    public string Source { get; init; } = "";
    public string Actor { get; init; } = "";
    public Guid PaymentId { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; private set; }
    public string Outcome { get; private set; } = "Pending";
    public string Kind { get; init; } = "Submit";
    public Guid? CommandId { get; init; }

    /// <summary>Completes one durable bank interaction exactly once with its observed outcome and UTC time.</summary>
    public void Complete(BankOutcome outcome, DateTime at)
    {
        if (CompletedAt is not null) throw new DomainException("Attempt is already complete.");
        CompletedAt = at;
        Outcome = outcome.ToString();
    }
}
