using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

/// <summary>Fictional matching-currency corporate account reference data shared by EF seeding and bank fixtures.</summary>
public static class DemoFundingAccounts
{
    /// <summary>Creates GBP/EUR/USD source accounts without balances, credentials or implied FX.</summary>
    public static FundingAccount[] Create() => new[] { ("company-uk", "GB"), ("company-de", "DE"), ("company-us", "US") }
        .SelectMany(owner => new[] { ("GBP", "bank-a", "a"), ("EUR", "bank-a", "a"), ("EUR", "bank-b", "b"), ("USD", "bank-b", "b") }
            .Select(account => new FundingAccount($"funding-{owner.Item1}-{account.Item1.ToLowerInvariant()}-{account.Item3}",
                "demo-tenant", owner.Item1, account.Item1, owner.Item2, account.Item2,
                $"mock-account-{owner.Item1}-{account.Item1.ToLowerInvariant()}-{account.Item3}"))).ToArray();
}
