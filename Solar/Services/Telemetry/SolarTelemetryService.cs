using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Solar.Data;
using Solar.Entities;
using Solar.Models;
using Solar.Options;

namespace Solar.Services.Telemetry;

/// <summary>
/// Read service implementation providing telemetry histories, daily metrics, and database statistics.
/// </summary>
public class SolarTelemetryService : ISolarTelemetryService
{
    private readonly SolarDbContext _dbContext;
    private readonly SolarPlatformOptions _options;

    public SolarTelemetryService(
        SolarDbContext dbContext,
        IOptions<SolarPlatformOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<InverterReading?> GetLatestReadingAsync(string? deviceId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.InverterReadings.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            query = query.Where(r => r.DeviceId == deviceId);
        }

        return await query.OrderByDescending(r => r.Timestamp).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<InverterReading>> GetReadingsHistoryAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.InverterReadings
            .AsNoTracking()
            .Where(r => r.Timestamp >= from && r.Timestamp <= to);

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            query = query.Where(r => r.DeviceId == deviceId);
        }

        return await query.OrderBy(r => r.Timestamp).ToListAsync(cancellationToken);
    }

    public async Task<List<DailyEnergyMetric>> GetDailyMetricsAsync(
        DateOnly fromDate,
        DateOnly toDate,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DailyEnergyMetrics
            .AsNoTracking()
            .Where(m => m.Date >= fromDate && m.Date <= toDate);

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            query = query.Where(m => m.DeviceId == deviceId);
        }

        return await query.OrderBy(m => m.Date).ToListAsync(cancellationToken);
    }

    public async Task<SystemStatusDto> GetSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        long totalReadingsCount = await _dbContext.InverterReadings.LongCountAsync(cancellationToken);
        long totalDailyRecordsCount = await _dbContext.DailyEnergyMetrics.LongCountAsync(cancellationToken);
        var latestReading = await GetLatestReadingAsync(_options.DeviceId, cancellationToken);

        return new SystemStatusDto
        {
            Status = "Operational",
            SyncIntervalMinutes = _options.SyncIntervalMinutes,
            ConfiguredStationId = _options.StationId,
            ConfiguredDeviceId = _options.DeviceId,
            ConfiguredTimeZone = _options.TimeZone,
            Database = new DatabaseStatusDto
            {
                TotalInverterReadings = totalReadingsCount,
                TotalDailyMetricRecords = totalDailyRecordsCount,
                LatestReadingTimestampUtc = latestReading?.Timestamp,
                LatestAcInputVoltage = latestReading?.AcInputVoltage,
                LatestGridPowerWatts = latestReading?.GridPowerWatts,
                LatestLoadPowerWatts = latestReading?.LoadPowerWatts,
                IsGridAvailable = latestReading?.IsGridAvailable,
                BatterySoc = latestReading?.BatterySoc
            },
            ServerTimeUtc = DateTimeOffset.UtcNow
        };
    }

    public async Task<List<MonthlyEnergyMetricDto>> GetMonthlyMetricsAsync(
        int? year = null,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DailyEnergyMetrics.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            query = query.Where(m => m.DeviceId == deviceId);
        }

        if (year.HasValue)
        {
            query = query.Where(m => m.Date.Year == year.Value);
        }

        var dailyMetrics = await query.ToListAsync(cancellationToken);

        return dailyMetrics
            .GroupBy(m => new { m.DeviceId, m.Date.Year, m.Date.Month })
            .Select(g =>
            {
                double totalLoad = g.Sum(x => x.TotalLoadEnergyKwh);
                double gridImport = g.Sum(x => x.GridImportEnergyKwh);
                double pvGen = g.Sum(x => x.PvGenerationEnergyKwh);
                double uptime = g.Sum(x => x.GridUptimeHours);
                double outage = g.Sum(x => x.GridOutageHours);
                double totalHours = uptime + outage;
                double availabilityPct = totalHours > 0 ? Math.Round((uptime / totalHours) * 100.0, 2) : 0.0;
                int daysCount = g.Count();

                var monthDate = new DateTime(g.Key.Year, g.Key.Month, 1);

                return new MonthlyEnergyMetricDto
                {
                    DeviceId = g.Key.DeviceId,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = monthDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                    TotalLoadEnergyKwh = Math.Round(totalLoad, 4),
                    GridImportEnergyKwh = Math.Round(gridImport, 4),
                    PvGenerationEnergyKwh = Math.Round(pvGen, 4),
                    GridUptimeHours = Math.Round(uptime, 2),
                    GridOutageHours = Math.Round(outage, 2),
                    GridAvailabilityPercentage = availabilityPct,
                    DaysRecorded = daysCount,
                    AverageDailyLoadKwh = daysCount > 0 ? Math.Round(totalLoad / daysCount, 4) : 0.0,
                    AverageDailyPvKwh = daysCount > 0 ? Math.Round(pvGen / daysCount, 4) : 0.0
                };
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();
    }

    public async Task<List<DailyHourlyMetricDto>> GetDailyHourlyMetricsAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        DateOnly to = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly from = fromDate ?? to.AddDays(-6);
        string targetDevice = !string.IsNullOrWhiteSpace(deviceId) ? deviceId : (_options.DeviceId ?? string.Empty);

        var dailyMetrics = await GetDailyMetricsAsync(from, to, targetDevice, cancellationToken);
        var dailyDict = dailyMetrics.ToDictionary(m => m.Date);

        var timeZone = ResolveTimeZone();
        DateTime localStart = from.ToDateTime(TimeOnly.MinValue);
        DateTime localEnd = to.ToDateTime(TimeOnly.MaxValue);
        DateTimeOffset fromDto = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone));
        DateTimeOffset toDto = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone));

        var readings = await GetReadingsHistoryAsync(fromDto, toDto, targetDevice, cancellationToken);

        // Group readings by local date and hour in station time zone
        var readingsByDateHour = readings
            .Select(r =>
            {
                var localTime = TimeZoneInfo.ConvertTime(r.Timestamp, timeZone);
                return new
                {
                    Date = DateOnly.FromDateTime(localTime.DateTime),
                    Hour = localTime.Hour,
                    r.GridPowerWatts,
                    r.PvPowerWatts,
                    r.LoadPowerWatts,
                    r.IsGridAvailable
                };
            })
            .GroupBy(r => new { r.Date, r.Hour })
            .ToDictionary(
                g => (g.Key.Date, g.Key.Hour),
                g => new
                {
                    AvgGrid = g.Average(x => x.GridPowerWatts),
                    AvgPv = g.Average(x => x.PvPowerWatts),
                    AvgLoad = g.Average(x => x.LoadPowerWatts),
                    AnyGridAvail = g.Any(x => x.IsGridAvailable)
                });

        var result = new List<DailyHourlyMetricDto>();

        for (DateOnly date = from; date <= to; date = date.AddDays(1))
        {
            dailyDict.TryGetValue(date, out var daily);
            var hoursList = new List<HourlyPointDto>();

            for (int h = 0; h < 24; h++)
            {
                if (readingsByDateHour.TryGetValue((date, h), out var rh))
                {
                    hoursList.Add(new HourlyPointDto
                    {
                        Hour = h,
                        GridPowerWatts = Math.Round(Math.Max(0, rh.AvgGrid), 1),
                        PvPowerWatts = Math.Round(Math.Max(0, rh.AvgPv), 1),
                        LoadPowerWatts = Math.Round(Math.Max(0, rh.AvgLoad), 1),
                        IsGridAvailable = rh.AnyGridAvail
                    });
                }
                else
                {
                    // No dummy or synthesized data: exactly 0 for unrecorded hours
                    hoursList.Add(new HourlyPointDto
                    {
                        Hour = h,
                        GridPowerWatts = 0.0,
                        PvPowerWatts = 0.0,
                        LoadPowerWatts = 0.0,
                        IsGridAvailable = false
                    });
                }
            }

            var dt = date.ToDateTime(TimeOnly.MinValue);
            double peakGrid = hoursList.Count > 0 ? hoursList.Max(x => x.GridPowerWatts) : 0.0;
            double peakPv = hoursList.Count > 0 ? hoursList.Max(x => x.PvPowerWatts) : 0.0;
            double peakLoad = hoursList.Count > 0 ? hoursList.Max(x => x.LoadPowerWatts) : 0.0;
            double totalUptime = daily?.GridUptimeHours ?? 0.0;
            double totalOutage = daily?.GridOutageHours ?? 0.0;
            double totalHours = totalUptime + totalOutage;
            double availPct = (totalHours > 0) ? Math.Round((totalUptime / totalHours) * 100.0, 1) : 0.0;

            result.Add(new DailyHourlyMetricDto
            {
                DeviceId = targetDevice,
                Date = date,
                FormattedDate = dt.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture),
                DayOfWeek = dt.ToString("ddd", CultureInfo.InvariantCulture),
                Hours = hoursList,
                PeakGridPowerWatts = peakGrid,
                PeakPvPowerWatts = peakPv,
                PeakLoadPowerWatts = peakLoad,
                TotalGridEnergyKwh = daily != null ? Math.Round(daily.GridImportEnergyKwh, 4) : 0.0,
                TotalPvEnergyKwh = daily != null ? Math.Round(daily.PvGenerationEnergyKwh, 4) : 0.0,
                TotalLoadEnergyKwh = daily != null ? Math.Round(daily.TotalLoadEnergyKwh, 4) : 0.0,
                GridUptimeHours = Math.Round(totalUptime, 2),
                GridOutageHours = Math.Round(totalOutage, 2),
                GridAvailabilityPercentage = availPct
            });
        }

        return result;
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
}

