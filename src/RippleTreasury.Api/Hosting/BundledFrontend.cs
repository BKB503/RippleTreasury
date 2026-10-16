using Microsoft.Extensions.FileProviders;

namespace RippleTreasury.Api;

/// <summary>Serves the compiled SPA from build/publish output on the API origin.</summary>
public static class BundledFrontend
{
    /// <summary>Serves built UI assets with cache rules and SPA fallback while preserving API/health/asset 404 behavior.</summary>
    public static void UseBundledFrontend(this WebApplication app)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (!File.Exists(Path.Combine(folder, "index.html")))
        {
            throw new InvalidOperationException("React bundle missing. Build or publish RippleTreasury.Api before starting it.");
        }

        var provider = new PhysicalFileProvider(folder);
        app.Lifetime.ApplicationStopped.Register(provider.Dispose);
        var options = new StaticFileOptions
        {
            FileProvider = provider,
            OnPrepareResponse = context =>
            {
                context.Context.Response.Headers.CacheControl = context.Context.Request.Path.StartsWithSegments("/assets")
                    ? "public,max-age=31536000,immutable" : "no-cache";
            }
        };
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
        app.UseStaticFiles(options);
        // Serve real assets before routing selects reserved-path fallback endpoints.
        app.UseRouting();

        // Unknown API/operational routes must never return the SPA's HTML with HTTP 200.
        foreach (var prefix in new[] { "api", "health", "swagger", "assets" })
        {
            app.MapFallback($"/{prefix}/{{**path}}", () => Results.NotFound());
        }

        app.MapFallbackToFile("index.html", options);
    }
}
