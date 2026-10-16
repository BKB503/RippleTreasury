using Microsoft.AspNetCore.Mvc;
using RippleTreasury.Application;
using RippleTreasury.Contracts;

namespace RippleTreasury.Api.Controllers;

using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

[ApiController]
[Route("mock-banks/{provider}/executions")]
public class MockBanksController(MockBankEngine bank, IHostEnvironment environment) : ControllerBase
{
    /// <summary>Exposes the Development-only simulated bank, including a lost HTTP response after durable bank execution.</summary>
    [HttpPost]

    public async Task<IActionResult> Submit(string provider, [FromBody] MockSubmission request, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();
        var observation = await bank.Submit(provider, request, ct);
        return observation.Outcome == BankOutcome.Unknown && request.Scenario == "lost-response"
            ? StatusCode(504) : Ok(observation);
    }
    /// <summary>Exposes Development-only evidence lookup for the same provider execution key.</summary>

    [HttpGet("{id:guid}")]

    public async Task<ActionResult<BankObservation>> Lookup(string provider, Guid id, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();
        return await bank.Lookup(provider, id, ct);
    }
}
