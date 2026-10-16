namespace RippleTreasury.Domain;

public enum PaymentAuditAction
{
    InstructionCreated,
    SubmissionStarted,
    SubmissionObserved,
    ReconciliationStarted,
    ReconciliationCompleted,
    SubmissionLeaseExpired,
    ReconciliationLeaseExpired
}
