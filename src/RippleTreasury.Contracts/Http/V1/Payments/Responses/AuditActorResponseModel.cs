namespace RippleTreasury.Contracts;

/// <summary>Recorded identity, display name and kind of the initiating person or system.</summary>
public record AuditActorResponseModel(
    string Id,
    string DisplayName,
    string Kind);
