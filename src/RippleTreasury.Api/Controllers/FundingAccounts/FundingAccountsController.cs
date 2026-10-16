using Microsoft.AspNetCore.Mvc;
using RippleTreasury.Application;
using RippleTreasury.Contracts;

namespace RippleTreasury.Api.Controllers;

/// <summary>Corporate account choices for the existing unauthenticated demo tenant.</summary>
[ApiController]
[Route("api/v1/funding-accounts")]
public class FundingAccountsController(IFundingAccountQueries queries) : ControllerBase
{
    /// <summary>Lists source-account metadata; production must resolve tenant from validated claims.</summary>
    [HttpGet]
    public Task<FundingAccountResponseModel[]> List(CancellationToken ct) => queries.List("demo-tenant", ct);
}
