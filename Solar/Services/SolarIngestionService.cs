using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Solar.Data;
using Solar.Entities;
using Solar.Models;
using Solar.Options;
using Solar.Services.Calculations;
using Solar.Services.Parsers;

namespace Solar.Services;

/// <summary>
/// Orchestrates data ingestion, deduplication, and metric computation for solar telemetry.
/// </summary>
public class SolarIngestionService : ISolarIngestionService
{
    private static string? _cachedResolvedDeviceId;

    private readonly SolarDbContext _dbContext;
    private readonly ISolarPlatformClient _platformClient;
    private readonly ISiseliDataParser _parser;
    private readonly IEnergyCalculationService _calculationService;
    private readonly SolarPlatformOptions _options;
    private readonly ILogger<SolarIngestionService> _logger;

    public SolarIngestionService(
        SolarDbContext dbContext,
        ISolarPlatformClient platformClient,
        ISiseliDataParser parser,
        IEnergyCalculationService calculationService,
        IOptions<SolarPlatformOptions> options,
        ILogger<SolarIngestionService> logger)
    {
        _dbContext = dbContext;
        _platformClient = platformClient;
        _parser = parser;
        _calculationService = calculationService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SyncResultDto> IngestDataAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("SolarIngestionService: Starting ingestion run...");

        // 1. Resolve Device ID
        string deviceId = await ResolveDeviceIdAsync(cancellationToken);
        if (string.IsNullOrEmpty(deviceId))
        {
            return new SyncResultDto
            {
                Success = false,
                Message = "Unable to resolve inverter DeviceId.",
                Duration = stopwatch.Elapsed
            };
        }

        TimeZoneInfo timeZone = ResolveTimeZone();

        // 2. Determine target query window
        var latestStored = await _dbContext.InverterReadings
            .AsNoTracking()
            .Where(r => r.DeviceId == deviceId)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        DateTimeOffset toTime = DateTimeOffset.UtcNow;
        DateTimeOffset fromTime = latestStored != null
            ? latestStored.Timestamp.AddMinutes(-15) // small overlap for boundary deduplication
            : toTime.AddHours(-24); // Initial catch-up for past 24 hours

        _logger.LogInformation("SolarIngestionService: Fetching telemetry for Device {DeviceId} from {From} to {To}...",
            deviceId, fromTime, toTime);

        // 3. Query historical time-series data
        var parsedReadings = new List<InverterReading>();
        var timeSeriesPayload = await _platformClient.GetTimeSeriesDataAsync(deviceId, fromTime, toTime, cancellationToken);

        if (timeSeriesPayload != null && timeSeriesPayload.TimeSeries.Count > 0)
        {
            parsedReadings = _parser.ParseTimeSeriesReadings(deviceId, timeSeriesPayload);
            _logger.LogInformation("SolarIngestionService: Retrieved {Count} time-series readings from API.", parsedReadings.Count);
        }
        else
        {
            _logger.LogInformation("SolarIngestionService: No time-series data found for range. Falling back to live energy flow...");
            var energyFlow = await _platformClient.GetLatestEnergyFlowAsync(deviceId, cancellationToken);
            if (energyFlow != null)
            {
                var reading = _parser.ParseEnergyFlowReading(deviceId, energyFlow);
                if (reading != null)
                {
                    parsedReadings.Add(reading);
                }
            }
        }

        // 4. Deduplicate readings against database
        int newReadingsCount = 0;
        if (parsedReadings.Count > 0)
        {
            var candidateTimestamps = parsedReadings.Select(r => r.Timestamp).ToList();
            var minTimestamp = candidateTimestamps.Min();
            var maxTimestamp = candidateTimestamps.Max();

            var existingTimestamps = await _dbContext.InverterReadings
                .AsNoTracking()
                .Where(r => r.DeviceId == deviceId && r.Timestamp >= minTimestamp && r.Timestamp <= maxTimestamp)
                .Select(r => r.Timestamp)
                .ToListAsync(cancellationToken);

            var existingSet = new HashSet<DateTimeOffset>(existingTimestamps);
            var readingsToInsert = parsedReadings
                .Where(r => !existingSet.Contains(r.Timestamp))
                .GroupBy(r => r.Timestamp) // ensure no duplicates within the batch
                .Select(g => g.First())
                .ToList();

            if (readingsToInsert.Count > 0)
            {
                await _dbContext.InverterReadings.AddRangeAsync(readingsToInsert, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                newReadingsCount = readingsToInsert.Count;
                _logger.LogInformation("SolarIngestionService: Successfully inserted {Count} new telemetry readings.", newReadingsCount);
            }
            else
            {
                _logger.LogInformation("SolarIngestionService: All retrieved readings already exist in the database.");
            }
        }

        // 5. Calculate and update DailyEnergyMetrics for affected dates
        DailyEnergyMetric? todayMetric = null;
        var affectedDates = parsedReadings
            .Select(r => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(r.Timestamp, timeZone).DateTime))
            .Distinct()
            .ToList();

        // Always include today's local date
        var todayLocalDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
        if (!affectedDates.Contains(todayLocalDate))
        {
            affectedDates.Add(todayLocalDate);
        }

        foreach (var date in affectedDates)
        {
            var metric = await RecomputeDailyMetricAsync(deviceId, date, timeZone, cancellationToken);
            if (date == todayLocalDate)
            {
                todayMetric = metric;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var latestReading = await _dbContext.InverterReadings
            .AsNoTracking()
            .Where(r => r.DeviceId == deviceId)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation("SolarIngestionService: Ingestion completed in {Duration} ms. Ingested {Count} readings.",
            stopwatch.ElapsedMilliseconds, newReadingsCount);

        return new SyncResultDto
        {
            Success = true,
            Message = $"Ingested {newReadingsCount} new telemetry readings.",
            DeviceId = deviceId,
            SyncTimestampUtc = DateTimeOffset.UtcNow,
            TelemetryReadingsIngested = newReadingsCount,
            LatestReading = latestReading,
            TodayMetric = todayMetric,
            Duration = stopwatch.Elapsed
        };
    }

    private async Task<string> ResolveDeviceIdAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.DeviceId))
        {
            return _options.DeviceId;
        }

        if (!string.IsNullOrWhiteSpace(_cachedResolvedDeviceId))
        {
            return _cachedResolvedDeviceId;
        }

        var devices = await _platformClient.GetDevicesAsync(_options.StationId, cancellationToken);
        if (devices.Count > 0)
        {
            _cachedResolvedDeviceId = devices[0].Id;
            return _cachedResolvedDeviceId;
        }

        return string.Empty;
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_options.TimeZone))
            {
                return TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone);
            }
        }
        catch
        {
            try
            {
                // Fallback for Windows ID
                return TimeZoneInfo.FindSystemTimeZoneById("Syria Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }

        return TimeZoneInfo.Utc;
    }

    private async Task<DailyEnergyMetric> RecomputeDailyMetricAsync(
        string deviceId,
        DateOnly date,
        TimeZoneInfo timeZone,
        CancellationToken cancellationToken)
    {
        var localStart = date.ToDateTime(TimeOnly.MinValue);
        var localEnd = date.ToDateTime(TimeOnly.MaxValue);

        var utcStart = new DateTimeOffset(localStart, timeZone.GetUtcOffset(localStart)).ToUniversalTime();
        var utcEnd = new DateTimeOffset(localEnd, timeZone.GetUtcOffset(localEnd)).ToUniversalTime();

        var dayReadings = await _dbContext.InverterReadings
            .AsNoTracking()
            .Where(r => r.DeviceId == deviceId && r.Timestamp >= utcStart && r.Timestamp <= utcEnd)
            .OrderBy(r => r.Timestamp)
            .ToListAsync(cancellationToken);

        var calculatedMetric = _calculationService.CalculateDailyMetric(deviceId, date, dayReadings);

        var existingMetric = await _dbContext.DailyEnergyMetrics
            .FirstOrDefaultAsync(m => m.DeviceId == deviceId && m.Date == date, cancellationToken);

        if (existingMetric == null)
        {
            existingMetric = calculatedMetric;
            await _dbContext.DailyEnergyMetrics.AddAsync(existingMetric, cancellationToken);
        }
        else
        {
            existingMetric.TotalLoadEnergyKwh = calculatedMetric.TotalLoadEnergyKwh;
            existingMetric.GridImportEnergyKwh = calculatedMetric.GridImportEnergyKwh;
            existingMetric.PvGenerationEnergyKwh = calculatedMetric.PvGenerationEnergyKwh;
            existingMetric.GridUptimeHours = calculatedMetric.GridUptimeHours;
            existingMetric.GridOutageHours = calculatedMetric.GridOutageHours;
            existingMetric.TelemetrySampleCount = calculatedMetric.TelemetrySampleCount;
            existingMetric.LastCalculatedAtUtc = calculatedMetric.LastCalculatedAtUtc;
        }

        _logger.LogInformation("SolarIngestionService: Date {Date} summary -> Load: {Load:F2} kWh, Grid Import: {Grid:F2} kWh, Grid Uptime: {Uptime:F2} hrs.",
            date, existingMetric.TotalLoadEnergyKwh, existingMetric.GridImportEnergyKwh, existingMetric.GridUptimeHours);

        return existingMetric;
    }
}
