using Microsoft.AspNetCore.Mvc;
using RippleTreasury.Application;
using RippleTreasury.Contracts;

namespace RippleTreasury.Api.Controllers;

[ApiController]
[Route("api/v1/reconciliation-cases")]
public class ReconciliationCasesController(IReconciliationQueries queries, ReconciliationService processor) : ControllerBase
{
    /// <summary>Returns the investigation queue with optional batch/status filtering.</summary>
    [HttpGet]

    public Task<PagedResponseModel<ReconciliationCaseResponseModel>> List(CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, [FromQuery] Guid? batchId = null) => queries.Cases(page, pageSize, status, ct, batchId);
    /// <summary>Returns case and payment evidence for operator inspection.</summary>

    [HttpGet("{id:guid}")]

    public async Task<ReconciliationCaseDetailResponseModel> Get(Guid id, CancellationToken ct)
    {
        var detail = await queries.Case(id, ct);
        Response.Headers.ETag = $"\"{detail.Case.Version}\"";
        return detail;
    }
    /// <summary>Validates the version precondition and invokes safe provider lookup, never a new transfer.</summary>

    [HttpPost("{id:guid}/recheck")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]

    public async Task<IActionResult> Recheck(Guid id, [FromBody] RecheckReconciliationRequestModel input, [FromHeader(Name = "If-Match")] string ifMatch, CancellationToken ct)
    {
        if (!int.TryParse(ifMatch.Trim('"'), out var version))
            throw new InputException(new()
            {
                ["If-Match"] = ["Expected case version required."]
            });
        await processor.Recheck(id, version, input.CommandId, ct);
        return Ok(new
        {
            message = "Provider lookup completed"
        });
    }
}
