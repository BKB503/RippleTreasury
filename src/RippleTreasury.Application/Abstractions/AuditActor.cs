namespace RippleTreasury.Application;

/// <summary>Identity snapshot of the person or system responsible for a business action.</summary>
public record AuditActor(string Id, string DisplayName, string Kind)
{

    /// <summary>Creates a system actor for automated work without attributing it to a person.</summary>
    public static AuditActor System(string id) => new(id, id, "System");
}
