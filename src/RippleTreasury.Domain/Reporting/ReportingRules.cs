using System.Numerics;

namespace RippleTreasury.Domain;

public static class ReportingRules
{

    // Never combine currencies or overflow Int64 when a report contains many instructions.
    /// <summary>Adds integer minor units with arbitrary-precision totals, never combining different currencies.</summary>
    public static Dictionary<string, BigInteger> Totals(IEnumerable<PaymentInstruction> instructions) => instructions
        .GroupBy(x => x.CurrencyCode).OrderBy(x => x.Key)
        .ToDictionary(x => x.Key, x => x.Aggregate(BigInteger.Zero, (sum, payment) => sum + payment.AmountInMinorUnits));

    /// <summary>Rejects an inverted UTC receipt-date reporting range.</summary>
    public static void ValidatePeriod(DateTime? start, DateTime? end)
    {
        if (start > end)
            throw new DomainException("Invalid date range.");
    }

    /// <summary>Restricts reports to implemented status/provider groupings.</summary>
    public static void ValidateGrouping(string grouping)
    {
        if (grouping is not ("status" or "provider"))
            throw new DomainException("groupBy must be status or provider.");
    }
}
