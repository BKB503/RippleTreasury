using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Durable claimed-work context passed to the bank gateway after the claim transaction commits.</summary>
public record WorkItem(PaymentInstruction Instruction, PaymentExecution Execution, Guid AttemptId, string LeaseOwner, string CorrelationId, string? TraceParent, int AttemptNumber);
