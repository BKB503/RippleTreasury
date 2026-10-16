using Microsoft.AspNetCore.Mvc;
using RippleTreasury.Application;
using RippleTreasury.Contracts;

namespace RippleTreasury.Api.Controllers;

/// <summary>Displays deterministic mock-bank capabilities, not live bank availability.</summary>
[ApiController]
[Route("api/v1/bank-capabilities")]
public class BankCapabilitiesController(IBankCapabilityQueries queries) : ControllerBase
{
    /// <summary>Lists configured supported corridors and compatible funding-account IDs.</summary>
    [HttpGet]
    public BankCapabilityResponseModel[] List() => queries.List();
}
