using RippleTreasury.Contracts;

namespace RippleTreasury.Application;

/// <summary>Read-only application boundary for dashboard and grouped payment reports.</summary>
public interface IReportingQueries
{
    /// <summary>Returns date-filtered counts grouped by status or provider and separate currency totals.</summary>
    Task<PaymentReportResponseModel> Report(DateTime? from, DateTime? to, string groupBy, CancellationToken ct);

    /// <summary>Returns date-filtered dashboard counts and exact per-currency instruction totals.</summary>
    Task<OperationsSummaryResponseModel> Summary(DateTime? from, DateTime? to, CancellationToken ct);
}
