using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RippleTreasury.Api;
using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Api.Tests;

public class ExceptionMappingTests
{
    [Theory]
    [InlineData("validation", 400)]
    [InlineData("domain", 422)]
    [InlineData("idempotency", 409)]
    [InlineData("version", 409)]
    [InlineData("missing", 404)]
    [InlineData("request", 400)]
    public async Task Given_AKnownFailure_When_HandlingTheException_Then_ReturnStructuredProblemDetails(string kind, int expectedStatus)
    {
        // Given
        Exception error = kind switch
        {
            "validation" => new InputException(new() { ["amount"] = ["Required"] }),
            "domain" => new DomainException("Constraint failed"),
            "idempotency" => new ConflictException("Key conflict"),
            "version" => new VersionConflictException(),
            "missing" => new MissingException(),
            _ => new BadHttpRequestException("Invalid body")
        };
        using var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services, TraceIdentifier = "unit-trace" };
        context.Items["CorrelationId"] = "unit-correlation";
        context.Response.Body = new MemoryStream();

        // When
        var handled = await new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance).TryHandleAsync(context, error, default);

        // Then
        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(expectedStatus, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("unit-correlation", body.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal("unit-trace", body.RootElement.GetProperty("traceId").GetString());
        Assert.Equal("application/problem+json", context.Response.ContentType!.Split(';')[0]);
        if (kind == "validation")
            Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("amount", out _));
    }

    [Fact]
    public async Task Given_AnUnexpectedFailure_When_HandlingTheException_Then_RedactInternalDetails()
    {
        // Given
        using var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Items["CorrelationId"] = "unit-correlation";
        context.Response.Body = new MemoryStream();

        // When
        await new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance).TryHandleAsync(context, new InvalidOperationException("internal credentials must not escape"), default);
        context.Response.Body.Position = 0;
        var response = await new StreamReader(context.Response.Body).ReadToEndAsync();

        // Then
        Assert.Equal(500, context.Response.StatusCode);
        Assert.DoesNotContain("credentials", response);
        Assert.DoesNotContain("stack", response);
        Assert.Contains("internal_error", response);
    }
}
