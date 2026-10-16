using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public class ApiAndResilienceTests
{
    [Fact]

    public async Task SwaggerHealthProblemDetailsAndIdempotencyAreRealHttpContracts()
    {
        await using var fixture = new Fixture();
        using var factory = new ApiFactory(fixture.Path);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        var swagger = await client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/v1/payment-batches", swagger);
        Assert.Contains("Idempotency-Key", swagger);
        Assert.Contains("If-Match", swagger);
        client.DefaultRequestHeaders.Add("Idempotency-Key", "api-test");
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "api-correlation");
        var input = fixture.Input("accept", "lost-response");
        var first = await client.PostAsJsonAsync("/api/v1/payment-batches", input);
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal("api-correlation", first.Headers.GetValues("X-Correlation-ID").Single());
        var receipt = (await first.Content.ReadFromJsonAsync<PaymentBatchReceiptResponseModel>())!;
        var replay = await client.PostAsJsonAsync("/api/v1/payment-batches", input);
        Assert.True((await replay.Content.ReadFromJsonAsync<PaymentBatchReceiptResponseModel>())!.Replayed);
        var conflict = await client.PostAsJsonAsync("/api/v1/payment-batches", input with
        {
            Payments = [input.Payments[0] with { Amount = 20 }]
        });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType!.MediaType);
        Assert.Contains("correlationId", await conflict.Content.ReadAsStringAsync());
        var invalid = await client.PostAsJsonAsync("/api/v1/payment-batches", input with
        {
            Payments = [input.Payments[0] with { Amount = 0 }]
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("payments[0]", await invalid.Content.ReadAsStringAsync());
        var missing = await client.GetAsync($"/api/v1/payments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync($"/api/v1/payment-batches/{receipt.Id}/payments?pageSize=101")).StatusCode);
        var batch = await client.GetFromJsonAsync<PaymentBatchResponseModel>($"/api/v1/payment-batches/{receipt.Id}");
        Assert.Equal(DateTimeKind.Utc, batch!.ReceivedAt.Kind);
        var summary = await client.GetFromJsonAsync<OperationsSummaryResponseModel>("/api/v1/operations/summary");
        Assert.Equal(2, summary!.Total);
        Assert.Equal(2, summary.Totals.Length);
        var report = await client.GetFromJsonAsync<PaymentReportResponseModel>("/api/v1/reports/payments?groupBy=provider");
        Assert.Equal(1, report!.Counts["bank-a"]);
        Assert.Equal(1, report.Counts["bank-b"]);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync("/api/v1/reports/payments?groupBy=currency")).StatusCode);
    }
    [Fact]

    public async Task RealResiliencePipelineNeverRetriesPostButRetriesStatusGet()
    {
        await using var f = new Fixture();
        await f.Initialize();
        await f.Accept(f.Input("lost-response"));
        var item = (await f.Store.Claim("worker", f.Clock.Now.UtcDateTime, TimeSpan.FromSeconds(30), default))!;
        var handler = new CountingBankHandler();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTreasury(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Path"] = f.Path }).Build());
        services.AddHttpClient("bank-submit").ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddHttpClient("bank-lookup").ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();
        var gateway = provider.GetRequiredService<IBankGateway>();
        Assert.Equal(BankOutcome.Unknown, (await gateway.Submit(item, default)).Outcome);
        Assert.Equal(1, handler.Posts);
        Assert.Equal(BankOutcome.Accepted, (await gateway.Lookup(item, default)).Outcome);
        Assert.Equal(3, handler.Gets);
        Assert.Equal("test-correlation", handler.Correlation);
    }
}
