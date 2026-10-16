namespace RippleTreasury.Contracts;

/// <summary>Payment response with durable attempts, business audit events and actor history.</summary>
public record PaymentDetailResponseModel(
    PaymentResponseModel Payment,
    PaymentAttemptResponseModel[] Attempts,
    PaymentAuditEventResponseModel[] Audit,
    PaymentHistoryResponseModel? History = null);
