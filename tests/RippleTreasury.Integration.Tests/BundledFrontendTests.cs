using System.Net;
using System.Text.RegularExpressions;

namespace RippleTreasury.Integration.Tests;

public class BundledFrontendTests
{
    [Fact]
    public async Task BundledReactSupportsDeepLinksAndCachingWithoutHidingMissingApiRoutes()
    {
        await using var fixture = new Fixture();
        using var factory = new ApiFactory(fixture.Path);
        using var client = factory.CreateClient();
        var root = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, root.StatusCode);
        Assert.Equal("text/html", root.Content.Headers.ContentType!.MediaType);
        Assert.True(root.Headers.CacheControl!.NoCache);
        var html = await root.Content.ReadAsStringAsync();
        Assert.DoesNotContain("/@vite/client", html);
        var script = Regex.Match(html, "src=\"(/assets/[^\"]+\\.js)\"");
        Assert.True(script.Success);
        var bundle = await client.GetAsync(script.Groups[1].Value);
        Assert.Equal(HttpStatusCode.OK, bundle.StatusCode);
        Assert.Contains("immutable", bundle.Headers.CacheControl!.ToString());
        foreach (var route in new[] { "/batches", "/reconciliation", "/process-batch" })
        {
            Assert.Equal(html, await client.GetStringAsync(route));
        }
        foreach (var route in new[] { "/api/v1/missing", "/health/missing", "/swagger/missing", "/assets/missing.js" })
        {
            var missing = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.NotEqual("text/html", missing.Content.Headers.ContentType?.MediaType);
        }
    }
}
