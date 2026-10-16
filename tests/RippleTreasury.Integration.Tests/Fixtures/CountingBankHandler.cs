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

public sealed class CountingBankHandler : HttpMessageHandler
{
    public int Posts { get; private set; }
    public int Gets { get; private set; }
    public string? Correlation { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Correlation = request.Headers.GetValues("X-Correlation-ID").Single();
        if (request.Method == HttpMethod.Post)
        {
            Posts++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.GatewayTimeout));
        }
        Gets++;
        return Task.FromResult(Gets < 3 ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new BankObservation(BankOutcome.Accepted, "verified-lookup")) });
    }
}
