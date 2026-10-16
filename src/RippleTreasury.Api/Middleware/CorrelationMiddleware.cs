using System.Diagnostics;

namespace RippleTreasury.Api;

public class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{

    /// <summary>Validates or generates a support correlation ID and propagates it through the request/response pipeline.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers["X-Correlation-ID"].ToString();
        var correlation = incoming.Length is > 0 and <= 64 && incoming.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? incoming : Guid.NewGuid().ToString("N");
        context.Items["CorrelationId"] = correlation;
        context.Response.Headers["X-Correlation-ID"] = correlation;
        using var scope = logger.BeginScope("RequestCorrelationId={RequestCorrelationId} TraceId={TraceId}",
            correlation, Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);

        await next(context);
    }
}
