namespace RippleTreasury.Domain;

public static class PaymentPolicy
{
    public const int MaxAttempts = 3;
    public const string Version = "mock-v1";

    /// <summary>Maps provider evidence to guarded domain transitions and the bounded safe-retry policy.</summary>
    public static Transition Observe(PaymentExecution execution, BankObservation observation, int submissionCount) => observation.Outcome switch
    {
        BankOutcome.Accepted => execution.ObserveAccepted(execution.Version, observation.Evidence),
        BankOutcome.Rejected => execution.ObserveRejected(execution.Version, observation.Evidence),
        BankOutcome.ProvenNotExecuted when submissionCount < MaxAttempts => execution.ScheduleSafeRetry(execution.Version, observation.Evidence),
        BankOutcome.ProvenNotExecuted => execution.ObserveRejected(execution.Version, "Safe retry budget exhausted; no execution"),
        _ when execution.Status == PaymentStatus.Submitting => execution.MarkUnknown(execution.Version, observation.Evidence),
        _ => execution.Escalate(execution.Version)
    };

    /// <summary>Calculates the backoff for a proven-safe application-controlled retry.</summary>
    public static TimeSpan RetryDelay(int submissionCount) => TimeSpan.FromSeconds(Math.Pow(2, submissionCount));
    /// <summary>Identifies unresolved financial outcomes that need provider evidence.</summary>
    public static bool RequiresReconciliation(PaymentStatus status) => status is PaymentStatus.OutcomeUnknown or PaymentStatus.ReconciliationRequired;

    /// <summary>Derives investigation progress separately from financial execution status.</summary>
    public static string CaseStatus(PaymentStatus status) => status is PaymentStatus.Accepted or PaymentStatus.Rejected or PaymentStatus.RetryScheduled ? "Resolved" : "NeedsManualReview";
    /// <summary>Derives batch progress from child outcomes; completion means submission outcomes, not settlement.</summary>
    public static string BatchStatus(IEnumerable<PaymentStatus> states)
    {
        var statuses = states.ToArray();
        if (statuses.Any(RequiresReconciliation))
            return "ActionRequired";
        if (statuses.All(x => x == PaymentStatus.Ready))
            return "Accepted";
        if (statuses.Any(x => x is PaymentStatus.Ready or PaymentStatus.Submitting or PaymentStatus.RetryScheduled))
            return "Processing";
        return statuses.Any(x => x == PaymentStatus.Rejected) ? "SubmissionCompletedWithExceptions" : "SubmissionCompleted";
    }
}
