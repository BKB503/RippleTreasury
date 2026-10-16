using RippleTreasury.Application;

namespace RippleTreasury.Api;

public static class DemoUsers
{

    public static IReadOnlyList<AuditActor> All { get; } = Array.AsReadOnly(new[]
    {
        new AuditActor("alice.treasury", "Alice Morgan", "DemoUser"),
        new AuditActor("ben.operations", "Ben Patel", "DemoUser"),
        new AuditActor("cara.finance", "Cara Wilson", "DemoUser")
    });
}
