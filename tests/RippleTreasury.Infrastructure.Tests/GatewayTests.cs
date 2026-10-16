using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RippleTreasury.Application;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Infrastructure.Tests;

public class GatewayTests
{

    private static WorkItem Work()
    {
        var id = Guid.NewGuid();
        var execution = new PaymentExecution(id, Guid.NewGuid());
        execution.Claim(0);
        return new(new(id, Guid.NewGuid(), "p-1", new Money(1234, "EUR"), "Mock beneficiary", "DE", "bank-b", PaymentPurpose.LoanRepayment, new(2026, 10, 14), "accept"), execution, Guid.NewGuid(), "worker", "unit-correlation", null, 1);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(504)]
    [InlineData(429)]
    public async Task Given_AnUncertainHttpStatus_When_Submitting_Then_ReturnUnknown(int status)
    {
        // Given
        using var clients = new TestHttpClientFactory(new RecordingBankHandler(new((HttpStatusCode)status)));
        var gateway = new HttpBankGateway(clients, NullLogger<HttpBankGateway>.Instance);

        // When
        var observation = await gateway.Submit(Work(), default);

        // Then
        Assert.Equal(BankOutcome.Unknown, observation.Outcome);
        Assert.Equal("bank-submit", clients.Name);
    }

    [Fact]
    public async Task Given_AClaimedInstruction_When_Submitting_Then_SendExactIntentAndStableKey()
    {
        // Given
        var handler = new RecordingBankHandler(new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new BankObservation(BankOutcome.Accepted, "bank-proof"))
        });
        using var clients = new TestHttpClientFactory(handler);
        var item = Work();

        // When
        await new HttpBankGateway(clients, NullLogger<HttpBankGateway>.Instance).Submit(item, default);
        using var body = JsonDocument.Parse(handler.Body!);

        // Then
        Assert.Equal(item.Execution.ExecutionId.ToString(), handler.Key);
        Assert.Equal("unit-correlation", handler.Correlation);
        Assert.Equal(1234, body.RootElement.GetProperty("minorUnits").GetInt64());
        Assert.Equal("EUR", body.RootElement.GetProperty("currency").GetString());
        Assert.Equal("LoanRepayment", body.RootElement.GetProperty("purpose").GetString());
        Assert.Equal("/mock-banks/bank-b/executions", handler.Uri!.AbsolutePath);
    }

    [Fact]
    public async Task Given_ABankLookupNotFound_When_CheckingStatus_Then_DoNotPermitResubmission()
    {
        // Given
        using var clients = new TestHttpClientFactory(new RecordingBankHandler(new(HttpStatusCode.NotFound)));

        // When
        var observation = await new HttpBankGateway(clients, NullLogger<HttpBankGateway>.Instance).Lookup(Work(), default);

        // Then
        Assert.Equal(BankOutcome.Unknown, observation.Outcome);
        Assert.NotEqual(BankOutcome.ProvenNotExecuted, observation.Outcome);
    }

    [Fact]
    public async Task Given_ADefinitiveBankRejection_When_Submitting_Then_PreserveTheRejection()
    {
        // Given
        using var clients = new TestHttpClientFactory(new RecordingBankHandler(new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new BankObservation(BankOutcome.Rejected, "invalid beneficiary"))
        }));

        // When
        var observation = await new HttpBankGateway(clients, NullLogger<HttpBankGateway>.Instance).Submit(Work(), default);

        // Then
        Assert.Equal(BankOutcome.Rejected, observation.Outcome);
    }

    [Fact]
    public async Task Given_AMalformedBankResponse_When_Submitting_Then_ReturnUnknown()
    {
        // Given
        using var clients = new TestHttpClientFactory(new RecordingBankHandler(new(HttpStatusCode.OK)
        {
            Content = new StringContent("malformed-json")
        }));

        // When
        var observation = await new HttpBankGateway(clients, NullLogger<HttpBankGateway>.Instance).Submit(Work(), default);

        // Then
        Assert.Equal(BankOutcome.Unknown, observation.Outcome);
    }

    [Fact]
    public async Task Given_AResilienceTimeout_When_Submitting_Then_SurfaceTransportUncertainty()
    {
        // Given
        var handler = new RecordingBankHandler(new(HttpStatusCode.OK)) { Error = new Polly.Timeout.TimeoutRejectedException("timeout") };
        using var clients = new TestHttpClientFactory(handler);

        // When
        var submit = () => new HttpBankGateway(clients, NullLogger<HttpBankGateway>.Instance).Submit(Work(), default);

        // Then
        await Assert.ThrowsAsync<HttpRequestException>(submit);
    }

    [Fact]
    public void Given_AProviderFingerprint_When_IntentOrAttemptMetadataChanges_Then_OnlyFinancialIntentChangesTheHash()
    {
        // Given
        var request = MockSubmission.From(Work());

        // When: compute the stable fingerprint before varying attempt and intent fields.
        var originalFingerprint = request.Fingerprint();

        // Then
        Assert.Equal(originalFingerprint, (request with
        {
            AttemptNumber = 2,
            Scenario = "lost-response"
        }).Fingerprint());

        // Then
        Assert.NotEqual(originalFingerprint, (request with
        {
            MinorUnits = 1235
        }).Fingerprint());
        Assert.NotEqual(originalFingerprint, (request with
        {
            Beneficiary = "Changed beneficiary"
        }).Fingerprint());
    }
}
