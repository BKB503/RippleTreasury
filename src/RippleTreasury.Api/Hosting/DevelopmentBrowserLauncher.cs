using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace RippleTreasury.Api;

/// <summary>F5 opens React after readiness; Visual Studio opens Swagger via launchSettings.</summary>
public class DevelopmentBrowserLauncher(IHostApplicationLifetime lifetime, IServer server,
    ILogger<DevelopmentBrowserLauncher> logger) : BackgroundService
{
    /// <summary>Opens the local UI only after host startup when the Development launch setting explicitly enables it.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = lifetime.ApplicationStarted.Register(() => started.TrySetResult());
        await started.Task.WaitAsync(stoppingToken);
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
        if (address is null) return;

        var uri = new UriBuilder(address) { Path = "/" };
        if (uri.Host is "0.0.0.0" or "[::]") uri.Host = "localhost";
        try
        {
            Process.Start(new ProcessStartInfo(uri.Uri.AbsoluteUri) { UseShellExecute = true });
            logger.LogInformation("ReactBrowserOpened Url={Url}", uri.Uri.AbsoluteUri);
        }
        catch (Exception error)
        {
            logger.LogWarning("BrowserLaunchFailed FailureType={FailureType} Url={Url}", error.GetType().Name, uri.Uri.AbsoluteUri);
        }
    }
}
