using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public sealed class TestFactory(DbContextOptions<TreasuryDb> options) : IDbContextFactory<TreasuryDb>
{

    public TreasuryDb CreateDbContext() => new(options);
}
