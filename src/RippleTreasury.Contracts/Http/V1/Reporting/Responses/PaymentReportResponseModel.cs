namespace RippleTreasury.Contracts;

/// <summary>Counts grouped by status or provider, with separate per-currency instruction totals.</summary>
public record PaymentReportResponseModel(
    string GroupBy,
    Dictionary<string, int> Counts,
    CurrencyTotalResponseModel[] Totals,
    int Total);
