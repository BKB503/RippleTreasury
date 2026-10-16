using Microsoft.AspNetCore.Mvc;
using RippleTreasury.Application;
using RippleTreasury.Contracts;

namespace RippleTreasury.Api.Controllers;

[ApiController]
[Route("api/v1/payment-batches")]
public class PaymentBatchesController(IBatchService batchService, IBatchQueries queries, ILogger<PaymentBatchesController> logger) : ControllerBase
{
    /// <summary>Binds the v1 intake request and delegates through IBatchService, returning the durable batch resource.</summary>
    [HttpPost]
    [ProducesResponseType<PaymentBatchReceiptResponseModel>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]

    public async Task<ActionResult<PaymentBatchReceiptResponseModel>> Accept([FromBody] CreatePaymentBatchRequestModel input, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken ct)
    {
        var result = await batchService.Accept(input, "demo-tenant", idempotencyKey,
            (string)HttpContext.Items["CorrelationId"]!, System.Diagnostics.Activity.Current?.Id, ct);
        logger.LogInformation("BatchAccepted BatchId={BatchId} Replayed={Replayed}", result.Id, result.Replayed);
        return Accepted($"/api/v1/payment-batches/{result.Id}", result);
    }
    /// <summary>Returns application-owned paged batch summaries.</summary>

    [HttpGet]

    public Task<PagedResponseModel<PaymentBatchResponseModel>> List(CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null) => queries.Batches(page, pageSize, status, ct);
    /// <summary>Returns one batch summary without dispatching or modifying payments.</summary>

    [HttpGet("{id:guid}")]

    public Task<PaymentBatchResponseModel> Get(Guid id, CancellationToken ct) => queries.Batch(id, ct);
    /// <summary>Returns the requested stable page of batch payment responses.</summary>

    [HttpGet("{id:guid}/payments")]

    public Task<PagedResponseModel<PaymentResponseModel>> Payments(Guid id, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) => queries.Payments(id, page, pageSize, ct);
}
