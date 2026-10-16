using RippleTreasury.Domain;

namespace RippleTreasury.Application;

// Groups one payment's existing Domain entities for application response projection.
public record PaymentReadData(PaymentBatch Batch, PaymentInstruction Instruction, PaymentExecution Execution,
    PaymentAttempt[] Attempts, PaymentAuditEvent[] Audit);
