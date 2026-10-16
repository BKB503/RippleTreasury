using RippleTreasury.Domain;

namespace RippleTreasury.Application;

// A consistent read snapshot containing the existing Domain entities, not a second entity model.
public record BatchReadData(PaymentBatch Batch, PaymentInstruction[] Instructions, PaymentExecution[] Executions);
