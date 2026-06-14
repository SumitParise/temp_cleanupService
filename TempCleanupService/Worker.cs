using Microsoft.Extensions.Options;

namespace TempCleanupService;

public sealed class Worker(
    TempCleanupRunner cleanupRunner,
    IOptions<CleanupOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.GetInterval();
        logger.LogInformation(
            "Temp cleanup worker started. Cleanup interval: {Interval}.",
            interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await cleanupRunner.CleanAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected cleanup pass failure.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
