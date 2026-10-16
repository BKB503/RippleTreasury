using Microsoft.AspNetCore.Mvc;
using RippleTreasury.Application;
using RippleTreasury.Contracts;

namespace RippleTreasury.Api.Controllers;

[ApiController]
[Route("api/v1/reports/payments")]
public class PaymentReportsController(IReportingQueries queries) : ControllerBase
{
    /// <summary>Returns a validated status/provider report with currency-separated instruction totals.</summary>
    [HttpGet]

    public Task<PaymentReportResponseModel> Get(CancellationToken ct, [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null, [FromQuery] string groupBy = "status") => queries.Report(from, to, groupBy, ct);
}
