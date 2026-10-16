namespace RippleTreasury.Contracts;

/// <summary>Durable submission or lookup attempt with completion time and observed outcome.</summary>
public record PaymentAttemptResponseModel(
    Guid Id,
    Guid PaymentId,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string Outcome,
    string Kind,
    string? Provider = null,
    string? FundingAccountId = null,
    DateOnly RequestedExecutionDate = default,
    string? RoutingPolicyVersion = null);
