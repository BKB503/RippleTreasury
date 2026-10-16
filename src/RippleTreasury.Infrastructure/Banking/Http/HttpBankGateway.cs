using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

/// <summary>Translates provider HTTP responses into conservative financial observations.</summary>
public class HttpBankGateway : IBankGateway
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<HttpBankGateway> _logger;

    /// <summary>Connects named HttpClient policies and safe structured bank logging.</summary>
    public HttpBankGateway(IHttpClientFactory clients, ILogger<HttpBankGateway> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    /// <summary>Sends one persisted bank payload/key; non-success or lost transport is conservative uncertainty, not retry permission.</summary>
    public async Task<BankObservation> Submit(WorkItem item, CancellationToken ct)
    {
        using var scope = Scope(item);
        _logger.LogInformation("PaymentSubmissionStarted Provider={Provider}", item.Instruction.Provider);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"mock-banks/{item.Instruction.Provider}/executions") { Content = JsonContent.Create(MockSubmission.From(item)) };
        request.Headers.Add("Idempotency-Key", item.Execution.ExecutionId.ToString());
        request.Headers.Add("X-Correlation-ID", item.CorrelationId);
        // Submission retry policy disables unsafe HTTP retries; ambiguity is reconciled by the application.
        using var response = await Send("bank-submit", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("BankSubmissionAmbiguous HttpStatus={HttpStatus}", (int)response.StatusCode);
            return new(BankOutcome.Unknown, $"Ambiguous HTTP {(int)response.StatusCode}");
        }
        var result = await ReadObservation(response, ct);
        _logger.LogInformation("BankObservation Outcome={Outcome}", result.Outcome);
        return result;
    }

    /// <summary>Queries the original bank and execution key through the bounded safe GET policy.</summary>
    public async Task<BankObservation> Lookup(WorkItem item, CancellationToken ct)
    {
        using var scope = Scope(item);
        _logger.LogInformation("ReconciliationStarted Provider={Provider}", item.Instruction.Provider);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"mock-banks/{item.Instruction.Provider}/executions/{item.Execution.ExecutionId}");
        request.Headers.Add("X-Correlation-ID", item.CorrelationId);
        using var response = await Send("bank-lookup", request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new(BankOutcome.Unknown, "Lookup not found; no authority to resubmit");
        response.EnsureSuccessStatusCode();
        var result = await ReadObservation(response, ct);
        _logger.LogInformation("BankLookupObservation Outcome={Outcome}", result.Outcome);
        return result;
    }

    /// <summary>Sends through the named resilience policy and translates provider timeout failures for the application.</summary>
    private async Task<HttpResponseMessage> Send(string clientName, HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            return await _clients.CreateClient(clientName).SendAsync(request, ct);
        }
        catch (Exception error) when (error is Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException)
        {
            throw new HttpRequestException("Bank transport result is uncertain.", error);
        }
    }

    /// <summary>Parses provider evidence conservatively; malformed or absent evidence cannot prove execution failure.</summary>
    private static async Task<BankObservation> ReadObservation(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var observation = await response.Content.ReadFromJsonAsync<BankObservation>(ct);
            return observation is null || !Enum.IsDefined(observation.Outcome) || string.IsNullOrWhiteSpace(observation.Evidence)
                ? new(BankOutcome.Unknown, "Invalid provider observation") : observation;
        }
        catch (JsonException) { return new(BankOutcome.Unknown, "Malformed provider observation"); }
    }

    /// <summary>Adds non-sensitive bank-operation identifiers to the log scope.</summary>
    private IDisposable? Scope(WorkItem item) => _logger.BeginScope(
        "BatchId={BatchId} PaymentId={PaymentId} ExecutionId={ExecutionId} AttemptId={AttemptId} CorrelationId={CorrelationId}",
        item.Instruction.BatchId, item.Instruction.Id, item.Execution.ExecutionId, item.AttemptId, item.CorrelationId);
}
