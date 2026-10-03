using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Solar.Options;
using Solar.Services;

namespace Solar.HostedServices;

/// <summary>
/// Background hosted service that periodically triggers solar telemetry ingestion using PeriodicTimer.
/// </summary>
public class SolarIngestionHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SolarPlatformOptions _options;
    private readonly ILogger<SolarIngestionHostedService> _logger;
    private readonly SemaphoreSlim _executionLock = new(1, 1);

    public SolarIngestionHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<SolarPlatformOptions> options,
        ILogger<SolarIngestionHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int intervalMinutes = Math.Max(1, _options.SyncIntervalMinutes);
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        _logger.LogInformation(
            "SolarIngestionHostedService: Started. Scheduled to ingest every {Interval} minutes.",
            intervalMinutes);

        // Allow application startup to complete before the first ingestion run
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            await RunIngestionAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SolarIngestionHostedService: Initial ingestion run encountered an error.");
        }

        using var timer = new PeriodicTimer(interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await RunIngestionAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SolarIngestionHostedService: Error occurred during periodic ingestion run.");
            }
        }

        _logger.LogInformation("SolarIngestionHostedService: Background worker is stopping.");
    }

    private async Task RunIngestionAsync(CancellationToken cancellationToken)
    {
        if (!await _executionLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogWarning("SolarIngestionHostedService: Previous ingestion run is still executing. Skipping scheduled tick.");
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<ISolarIngestionService>();

            _logger.LogInformation("SolarIngestionHostedService: Triggering scheduled data ingestion...");
            var result = await ingestionService.IngestDataAsync(cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "SolarIngestionHostedService: Ingestion completed successfully. Ingested {Count} readings in {Duration:F1}s.",
                    result.TelemetryReadingsIngested,
                    result.Duration.TotalSeconds);
            }
            else
            {
                _logger.LogWarning("SolarIngestionHostedService: Ingestion completed with warning: {Message}", result.Message);
            }
        }
        finally
        {
            _executionLock.Release();
        }
    }

    public override void Dispose()
    {
        _executionLock.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
