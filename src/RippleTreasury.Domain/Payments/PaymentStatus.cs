namespace RippleTreasury.Domain;

public enum PaymentStatus
{
    Ready, Submitting, RetryScheduled, OutcomeUnknown, ReconciliationRequired, Accepted, Rejected, Cancelled, Settled
}
