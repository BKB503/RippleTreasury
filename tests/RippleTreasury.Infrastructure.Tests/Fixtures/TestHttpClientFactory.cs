using System.Net;

namespace RippleTreasury.Infrastructure.Tests;

public sealed class TestHttpClientFactory(RecordingBankHandler handler) : IHttpClientFactory, IDisposable
{

    private readonly HttpClient client = new(handler) { BaseAddress = new Uri("http://unit-bank.test/") };
    public string? Name { get; private set; }

    public HttpClient CreateClient(string name)
    {
        Name = name;
        return client;
    }

    public void Dispose() => client.Dispose();
}
