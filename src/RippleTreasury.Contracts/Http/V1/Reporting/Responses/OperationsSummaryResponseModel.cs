namespace RippleTreasury.Contracts;

/// <summary>Dashboard payment counts, provider counts and currency-separated instruction totals.</summary>
public record OperationsSummaryResponseModel(
    int Total,
    Dictionary<string, int> StatusCounts,
    CurrencyTotalResponseModel[] Totals,
    Dictionary<string, int>? ProviderCounts = null);
