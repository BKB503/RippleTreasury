using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RippleTreasury.Contracts;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public class TreasuryHttpTests
{
    [Fact]
    public async Task GivenUnsupportedCorridorInOneLine_WhenPostingBatch_ThenStructuredAtomicRejectionBeforeAnyDispatch()
    {
        await using var fixture = new Fixture();
        using var host = new ApiFactory(fixture.Path);
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "unsupported");
        var input = TreasuryData.Batch(fixture, 3);
        input = input with { Payments = [input.Payments[0], input.Payments[1] with { DestinationCountry = "US" }] };
        var response = await client.PostAsJsonAsync("/api/v1/payment-batches", input);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("routing_unsupported", body.RootElement.GetProperty("validationCodes").GetProperty("payments[1]").GetString());
        Assert.True(body.RootElement.TryGetProperty("correlationId", out _));
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Empty(await db.PaymentBatches.ToArrayAsync());
        Assert.Empty(await db.PaymentAuditEvents.ToArrayAsync());
        Assert.Empty(await db.SimulatedBankExecutions.ToArrayAsync());
    }

    [Fact]
    public async Task GivenFundingAndCapabilityDirectories_WhenQuerying_ThenExposeSafeTenantMetadataAndEnhancedResponse()
    {
        await using var fixture = new Fixture();
        using var host = new ApiFactory(fixture.Path);
        using var client = host.CreateClient();
        var sources = await client.GetFromJsonAsync<FundingAccountResponseModel[]>("/api/v1/funding-accounts");
        Assert.Equal(12, sources!.Length);
        Assert.Equal(3, sources.Select(x => x.Currency).Distinct().Count());
        var banks = await client.GetFromJsonAsync<BankCapabilityResponseModel[]>("/api/v1/bank-capabilities");
        Assert.Equal(2, banks!.Length);
        Assert.DoesNotContain("accountReference", await client.GetStringAsync("/api/v1/funding-accounts"));
        client.DefaultRequestHeaders.Add("Idempotency-Key", "enhanced");
        var result = await client.PostAsJsonAsync("/api/v1/payment-batches", TreasuryData.Batch(fixture, 3));
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
        var receipt = (await result.Content.ReadFromJsonAsync<PaymentBatchReceiptResponseModel>())!;
        var payments = (await client.GetFromJsonAsync<PagedResponseModel<PaymentResponseModel>>($"/api/v1/payment-batches/{receipt.Id}/payments"))!.Items;
        Assert.All(payments, x => Assert.NotNull(x.Treasury));
        Assert.Contains(payments, x => x.Currency == "EUR" && x.Treasury!.IsCrossBorder);
        var repository = new FundingAccountRepository(fixture.Factory);
        Assert.Null(await repository.Find(sources[0].Id, "other-tenant", default));
        Assert.Empty(await repository.List("other-tenant", default));
    }
}
