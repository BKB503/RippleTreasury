using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RippleTreasury.Application;

namespace RippleTreasury.Infrastructure;

/// <summary>Polls durable work until the host stops; each payment has an independent failure boundary.</summary>
public class DispatchWorker : BackgroundService
{
    private const int PaymentsPerCycle = 50;
    private readonly DispatchService _processor;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<DispatchWorker> _logger;

    private readonly string _owner = $"worker-{Guid.NewGuid():N}";

    /// <summary>Connects the existing dispatch use case, host lifetime and worker logger.</summary>
    public DispatchWorker(DispatchService processor, IHostApplicationLifetime lifetime, ILogger<DispatchWorker> logger)
    {
        _processor = processor;
        _lifetime = lifetime;
        _logger = logger;
    }

    /// <summary>Waits for host readiness, recovers stale work and dispatches bounded cycles with per-payment failure isolation.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The development mock bank uses this API listener; wait until the host is ready.
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = _lifetime.ApplicationStarted.Register(() => started.TrySetResult());
        await started.Task.WaitAsync(stoppingToken);

        using var scope = _logger.BeginScope("WorkerId={WorkerId}", _owner);
        _logger.LogInformation("DispatchWorkerStarted PaymentsPerCycle={PaymentsPerCycle}", PaymentsPerCycle);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _processor.Recover(stoppingToken);

                    // Bounded cycles share time with recovery and avoid spinning when the queue is empty.
                    for (var index = 0; index < PaymentsPerCycle && !stoppingToken.IsCancellationRequested; index++)
                    {
                        try
                        {
                            if (!await _processor.DispatchOne(_owner, stoppingToken))
                            {
                                break;
                            }
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            return;
                        }
                        catch (Exception error)
                        {
                            // The attempt remains durable; unrelated payments continue.
                            _logger.LogError(error, "PaymentDispatchFailed; durable attempt remains recoverable");
                        }
                    }

                    await Task.Delay(250, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception error)
                {
                    _logger.LogError(error, "WorkerIterationFailed; retrying infrastructure after delay");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        finally
        {
            _logger.LogInformation("DispatchWorkerStopped");
        }
    }
}
