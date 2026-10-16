namespace RippleTreasury.Contracts;

/// <summary>Append-only business transition with actor, action, reason, time and correlation.</summary>
public record PaymentAuditEventResponseModel(
    Guid Id,
    Guid PaymentId,
    DateTime At,
    string Before,
    string After,
    int Version,
    string Actor,
    string Reason,
    string CorrelationId,
    Guid? AttemptId,
    string ActorDisplayName = "",
    string ActorKind = "System",
    string Source = "",
    string PolicyVersion = "",
    string Action = "");
