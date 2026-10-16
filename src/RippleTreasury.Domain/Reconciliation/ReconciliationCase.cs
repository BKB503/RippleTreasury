namespace RippleTreasury.Domain;

/// <summary>Tracks an investigation separately from the payment's financial execution state.</summary>
public class ReconciliationCase
{
    public Guid Id { get; init; }
    public Guid PaymentId { get; init; }
    public string Status { get; private set; } = "Open";
    public int Version { get; private set; }
    public DateTime OpenedAt { get; init; }
    public Guid? ActiveAttemptId { get; private set; }
    public DateTime? LeaseUntil { get; private set; }

    /// <summary>Starts a version-checked lookup lease and remembers its durable attempt identity.</summary>
    public void BeginLookup(int expectedVersion, Guid attemptId, DateTime now)
    {
        ReconciliationRules.EnsureCanRecheck(Status, Version, expectedVersion);
        Status = "QueryingProvider";
        Version++;
        ActiveAttemptId = attemptId;
        LeaseUntil = now.AddSeconds(30);
    }

    /// <summary>Accepts only the active lookup result and derives investigation status from the payment outcome.</summary>
    public void CompleteLookup(Guid attemptId, PaymentStatus paymentStatus)
    {
        if (ActiveAttemptId != attemptId || Status != "QueryingProvider") throw new VersionConflictException();
        Status = PaymentPolicy.CaseStatus(paymentStatus);
        Version++;
        ActiveAttemptId = null;
        LeaseUntil = null;
    }

    /// <summary>Reopens an expired lookup for further evidence without submitting any payment.</summary>
    public void RecoverLookup(DateTime now)
    {
        if (Status != "QueryingProvider" || LeaseUntil is null || LeaseUntil > now) throw new DomainException("Lookup lease has not expired.");
        Status = "AwaitingEvidence";
        Version++;
        ActiveAttemptId = null;
        LeaseUntil = null;
    }

    /// <summary>Reopens the same investigation for a later uncertain attempt and advances its version.</summary>
    public void Reopen()
    {
        Status = "Open";
        Version++;
    }
}
