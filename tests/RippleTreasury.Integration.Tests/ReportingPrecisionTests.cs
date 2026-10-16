using RippleTreasury.Contracts;

namespace RippleTreasury.Integration.Tests;

public class ReportingPrecisionTests
{
    [Fact]

    public async Task PerCurrencyTotalsRemainExactBeyondInt64AndJavaScriptRanges()
    {
        await using var fixture = new Fixture();
        await fixture.Initialize();
        var input = fixture.Input("accept", "accept");
        input = input with
        {
            Payments = input.Payments.Select(p => p with { Amount = long.MaxValue / 100m, Currency = "GBP" }).ToArray()
        };
        await fixture.Accept(input);
        var summary = await fixture.Store.Summary(null, null, default);
        var total = Assert.Single(summary.Totals);
        Assert.Equal("GBP", total.Currency);
        Assert.Equal("18446744073709551614", total.MinorUnits);
    }
}
