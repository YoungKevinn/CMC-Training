using System.Threading.Channels;
using AssetManager.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssetManager.Workers;

// Background service that dequeues scan job IDs and runs them
public class ScanWorker : BackgroundService
{
    private readonly Channel<string> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScanWorker> _logger;

    public ScanWorker(
        Channel<string> channel,
        IServiceScopeFactory scopeFactory,
        ILogger<ScanWorker> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScanWorker started");

        await foreach (var jobId in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            // Create a new DI scope per job so scoped services (DbContext) are fresh
            using var scope = _scopeFactory.CreateScope();
            var scanService = scope.ServiceProvider.GetRequiredService<IScanService>();

            try
            {
                await scanService.ExecuteJobAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ScanWorker stopping — cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing scan job {JobId}", jobId);
            }
        }

        _logger.LogInformation("ScanWorker stopped");
    }
}
