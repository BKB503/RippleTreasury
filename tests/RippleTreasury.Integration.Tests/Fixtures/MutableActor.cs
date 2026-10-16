using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public class MutableActor : IAuditActorAccessor
{

    public AuditActor Current { get; set; } = new("alice", "Alice Morgan", "DemoUser");
}
