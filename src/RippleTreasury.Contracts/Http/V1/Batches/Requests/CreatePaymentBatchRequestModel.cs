namespace RippleTreasury.Contracts;

/// <summary>Request to atomically accept a client-referenced batch of payment instructions.</summary>
public record CreatePaymentBatchRequestModel(
    string ClientReference,
    PaymentInstructionRequestModel[] Payments);
