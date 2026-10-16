using System.Globalization;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Formats domain-calculated currency totals without losing integer precision.</summary>
internal static class CurrencyTotalsResponseMapper
{
    /// <summary>Formats domain-calculated totals as exact minor-unit strings, keeping currencies separate.</summary>
    public static CurrencyTotalResponseModel[] Map(IEnumerable<PaymentInstruction> instructions) =>
        ReportingRules.Totals(instructions)
            .Select(x => new CurrencyTotalResponseModel(x.Key, x.Value.ToString(CultureInfo.InvariantCulture)))
            .ToArray();
}
