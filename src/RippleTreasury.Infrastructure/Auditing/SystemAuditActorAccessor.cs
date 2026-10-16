using RippleTreasury.Application;

namespace RippleTreasury.Infrastructure;

public class SystemAuditActorAccessor : IAuditActorAccessor
{

    public AuditActor Current => AuditActor.System("system-client");
}
