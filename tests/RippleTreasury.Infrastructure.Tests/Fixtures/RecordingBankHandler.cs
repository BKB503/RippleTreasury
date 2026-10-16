using System.Net;

namespace RippleTreasury.Infrastructure.Tests;

public sealed class RecordingBankHandler(HttpResponseMessage response) : HttpMessageHandler
{
    public string? Body { get; private set; }
    public string? Key { get; private set; }
    public string? Correlation { get; private set; }
    public Uri? Uri { get; private set; }
    public Exception? Error { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (Error != null)
            throw Error;
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        Key = request.Headers.TryGetValues("Idempotency-Key", out var keys) ? keys.Single() : null;
        Correlation = request.Headers.GetValues("X-Correlation-ID").Single();
        Uri = request.RequestUri;
        return response;
    }
}
