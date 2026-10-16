namespace RippleTreasury.Application;

/// <summary>Host-supplied actor. Production implementation resolves a validated identity, never a caller-supplied name.</summary>
public interface IAuditActorAccessor
{
    /// <summary>Gets the initiating actor resolved by the host for the current operation.</summary>
    AuditActor Current
    {
        get;
    }
}
