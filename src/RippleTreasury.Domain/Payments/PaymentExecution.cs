namespace RippleTreasury.Domain;

public sealed class PaymentExecution
{
    public Guid Id { get; private set; }
    public Guid ExecutionId { get; private set; }
    public PaymentStatus Status { get; private set; }
    public int Version { get; private set; }
    public string? BankReference { get; private set; }

    public string? LeaseOwner { get; private set; }
    public DateTime? LeaseUntil { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public int SubmissionCount { get; private set; }

    /// <summary>Creates or restores the lifecycle owner with its stable bank key and concurrency version.</summary>
    public PaymentExecution(Guid id, Guid executionId, PaymentStatus status = PaymentStatus.Ready, int version = 0, string? bankReference = null)
    {
        Id = id;
        ExecutionId = executionId;
        Status = status;
        Version = version;
        BankReference = bankReference;
    }
    // Every lifecycle change checks both the current version and allowed source states.

    /// <summary>Applies only an allowed version-checked transition with an explicit business reason.</summary>
    private Transition Move(int expected, PaymentStatus target, string reason, params PaymentStatus[] allowed)
    {
        if (expected != Version)
            throw new VersionConflictException();
        if (!allowed.Contains(Status))
            throw new DomainException($"Transition {Status} to {target} is forbidden.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Transition evidence/reason is required.");
        var before = Status;
        Status = target;
        Version = checked(Version + 1);
        return new(before, target, Version, reason);
    }

    /// <summary>Moves eligible work into submission; the leased overload also records ownership and the attempt count.</summary>
    public Transition Claim(int expected) => Move(expected, PaymentStatus.Submitting, "Durable attempt created", PaymentStatus.Ready, PaymentStatus.RetryScheduled);
    /// <summary>Records bank acknowledgement and its reference without treating it as settlement.</summary>
    public Transition ObserveAccepted(int expected, string evidence)
    {
        var t = Move(expected, PaymentStatus.Accepted, evidence, PaymentStatus.Submitting, PaymentStatus.OutcomeUnknown, PaymentStatus.ReconciliationRequired);
        BankReference = evidence;
        return t;
    }

    /// <summary>Records a definitive rejection only from an allowed unresolved submission state.</summary>
    public Transition ObserveRejected(int expected, string evidence) => Move(expected, PaymentStatus.Rejected, evidence, PaymentStatus.Submitting, PaymentStatus.OutcomeUnknown, PaymentStatus.ReconciliationRequired);
    /// <summary>Records an ambiguous submission outcome that requires provider evidence before any retry.</summary>
    public Transition MarkUnknown(int expected, string reason) => Move(expected, PaymentStatus.OutcomeUnknown, reason, PaymentStatus.Submitting);

    /// <summary>Keeps inconclusive provider evidence open for further reconciliation.</summary>
    public Transition Escalate(int expected) => Move(expected, PaymentStatus.ReconciliationRequired, "Provider lookup inconclusive", PaymentStatus.OutcomeUnknown, PaymentStatus.ReconciliationRequired);
    /// <summary>Marks an unresolved payment retryable only when the caller supplies authoritative non-execution proof.</summary>
    public Transition ScheduleSafeRetry(int expected, string proof) => Move(expected, PaymentStatus.RetryScheduled, proof, PaymentStatus.Submitting, PaymentStatus.OutcomeUnknown, PaymentStatus.ReconciliationRequired);

    /// <summary>Creates a durable lease; the repository commits it before any bank call.</summary>
    public Transition Claim(int expected, string owner, DateTime now, TimeSpan lease)
    {
        var transition = Claim(expected);
        SubmissionCount++;
        LeaseOwner = owner;
        LeaseUntil = now + lease;
        return transition;
    }

    /// <summary>Only bank evidence can authorize acceptance, rejection or a safe retry.</summary>
    public Transition RecordObservation(BankObservation observation, DateTime now)
    {
        var transition = PaymentPolicy.Observe(this, observation, SubmissionCount);
        LeaseOwner = null;
        LeaseUntil = null;
        if (Status == PaymentStatus.RetryScheduled) NextAttemptAt = now + PaymentPolicy.RetryDelay(SubmissionCount);
        return transition;
    }

    /// <summary>Clears an expired lease and records uncertainty rather than authorizing a second transfer.</summary>
    public Transition RecoverExpiredLease(DateTime now)
    {
        if (LeaseUntil is null || LeaseUntil > now) throw new DomainException("Submission lease has not expired.");
        var transition = MarkUnknown(Version, "Expired submission lease: reconcile before retry");
        LeaseOwner = null;
        LeaseUntil = null;
        return transition;
    }

    /// <summary>Cancels only a ready instruction with the expected version and an explicit reason.</summary>
    public Transition Cancel(int expected, string reason) => Move(expected, PaymentStatus.Cancelled, reason, PaymentStatus.Ready);
}
