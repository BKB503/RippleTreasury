namespace RippleTreasury.Contracts;

/// <summary>Creation, last-update and reconciliation timestamps with their recorded actors.</summary>
public record PaymentHistoryResponseModel(
    DateTime CreatedAt,
    AuditActorResponseModel CreatedBy,
    DateTime LastUpdatedAt,
    AuditActorResponseModel LastUpdatedBy,
    DateTime? LastReconciliationStartedAt,
    DateTime? LastReconciliationCompletedAt,
    AuditActorResponseModel? LastReconciliationBy);
