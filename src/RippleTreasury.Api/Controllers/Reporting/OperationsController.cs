using Microsoft.AspNetCore.Mvc;
using RippleTreasury.Application;
using RippleTreasury.Contracts;

namespace RippleTreasury.Api.Controllers;

[ApiController]
[Route("api/v1/operations")]
public class OperationsController(IReportingQueries queries) : ControllerBase
{
    /// <summary>Returns the application dashboard summary for the supplied receipt-date range.</summary>
    [HttpGet("summary")]

    public Task<OperationsSummaryResponseModel> Summary(CancellationToken ct, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null) => queries.Summary(from, to, ct);
}
