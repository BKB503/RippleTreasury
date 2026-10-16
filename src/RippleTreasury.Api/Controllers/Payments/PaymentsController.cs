using Microsoft.AspNetCore.Mvc;
using RippleTreasury.Application;
using RippleTreasury.Contracts;

namespace RippleTreasury.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController(IPaymentQueries queries) : ControllerBase
{
    /// <summary>Returns payment intent, attempt and audit evidence through the application query port.</summary>
    [HttpGet("{id:guid}")]

    public async Task<PaymentDetailResponseModel> Get(Guid id, CancellationToken ct)
    {
        var detail = await queries.Payment(id, ct);
        Response.Headers.ETag = $"\"{detail.Payment.Version}\"";
        return detail;
    }
}
