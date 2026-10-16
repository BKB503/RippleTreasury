using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Api;

public class DatabaseHealth(IDbContextFactory<TreasuryDb> factory) : IHealthCheck
{

    /// <summary>Checks local database readiness without depending on an external bank response.</summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            return await db.Database.CanConnectAsync(ct) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Database unavailable");
        }
        catch (Exception) { return HealthCheckResult.Unhealthy("Database unavailable"); }
    }
}
// Development orchestration only. Production deploys the static UI independently.
