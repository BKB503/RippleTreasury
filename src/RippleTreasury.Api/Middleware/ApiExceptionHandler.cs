using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Api;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{

    /// <summary>Maps errors centrally to redacted ProblemDetails and logs safe treasury validation codes with trace/correlation context.</summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            InputException => 400,
            DomainException => 422,
            ConflictException or VersionConflictException or DbUpdateConcurrencyException => 409,
            MissingException => 404,
            BadHttpRequestException => 400,
            _ => 500
        };
        if (status == 500)
            logger.LogError(exception, "Request failed TraceId={TraceId}", Activity.Current?.TraceId.ToString());
        if (exception is InputException rejected && rejected.ValidationCodes.Count > 0)
            logger.LogWarning("TreasuryIntakeRejected ValidationCodes={ValidationCodes} CorrelationId={CorrelationId} TraceId={TraceId}",
                string.Join(",", rejected.ValidationCodes.Values.Distinct().Order()), context.Items["CorrelationId"],
                Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);
        var problem = new ProblemDetails { Status = status, Title = status == 500 ? "Unexpected service error" : exception.Message, Type = $"https://httpstatuses.com/{status}" };
        problem.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        problem.Extensions["correlationId"] = context.Items["CorrelationId"];
        problem.Extensions["code"] = status == 500 ? "internal_error" : exception.GetType().Name;
        if (exception is InputException input)
        {
            problem.Extensions["errors"] = input.Errors;
            if (input.ValidationCodes.Count > 0) problem.Extensions["validationCodes"] = input.ValidationCodes;
        }
        context.Response.StatusCode = status;
        await Results.Problem(problem).ExecuteAsync(context);
        return true;
    }
}



