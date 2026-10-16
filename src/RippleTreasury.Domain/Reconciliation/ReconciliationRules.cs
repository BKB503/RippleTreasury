namespace RippleTreasury.Domain;

public static class ReconciliationRules
{

    /// <summary>Rejects stale versions and case states that cannot safely begin another lookup.</summary>
    public static void EnsureCanRecheck(string status, int currentVersion, int expectedVersion)
    {
        if (currentVersion != expectedVersion)
            throw new VersionConflictException();
        if (status is "Resolved" or "QueryingProvider")
            throw new DomainException("Case is resolved or provider lookup is already running.");
    }
}
