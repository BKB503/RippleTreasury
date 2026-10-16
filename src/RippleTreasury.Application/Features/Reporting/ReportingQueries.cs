using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Reporting use cases keep validation and response composition outside database adapters.</summary>
public class ReportingQueries(IPaymentReadRepository repository) : IReportingQueries
{
    /// <summary>Validates the date range and summarizes current payment/provider counts and exact currency-separated totals.</summary>
    public async Task<OperationsSummaryResponseModel> Summary(DateTime? from, DateTime? to, CancellationToken ct)
    {
        ReportingRules.ValidatePeriod(from, to);
        var batches = await repository.GetBatches(from, to, ct);
        var instructions = batches.SelectMany(x => x.Instructions).ToArray();
        var executions = batches.SelectMany(x => x.Executions).ToArray();
        return new(instructions.Length, executions.GroupBy(x => x.Status).ToDictionary(x => x.Key.ToString(), x => x.Count()),
            CurrencyTotalsResponseMapper.Map(instructions), instructions.GroupBy(x => x.Provider).ToDictionary(x => x.Key, x => x.Count()));
    }

    /// <summary>Validates the grouping and returns status- or provider-grouped counts with currency totals.</summary>
    public async Task<PaymentReportResponseModel> Report(DateTime? from, DateTime? to, string groupBy, CancellationToken ct)
    {
        ReportingRules.ValidateGrouping(groupBy);
        var summary = await Summary(from, to, ct);
        return new(groupBy, groupBy == "status" ? summary.StatusCounts : summary.ProviderCounts!, summary.Totals, summary.Total);
    }
}
