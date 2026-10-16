namespace RippleTreasury.Contracts;

/// <summary>Command identity for an audited provider-status recheck; the expected version is supplied separately.</summary>
public record RecheckReconciliationRequestModel(
    Guid CommandId);
