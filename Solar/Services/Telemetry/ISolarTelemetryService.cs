using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Solar.Entities;
using Solar.Models;

namespace Solar.Services.Telemetry;

/// <summary>
/// Read service contract for querying telemetry snapshots, aggregated metrics, and system status.
/// </summary>
public interface ISolarTelemetryService
{
    /// <summary>
    /// Retrieves the most recent inverter telemetry reading from the database.
    /// </summary>
    Task<InverterReading?> GetLatestReadingAsync(string? deviceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves historical telemetry readings within the specified timestamp interval.
    /// </summary>
    Task<List<InverterReading>> GetReadingsHistoryAsync(DateTimeOffset from, DateTimeOffset to, string? deviceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves daily energy and grid reliability metrics between two dates.
    /// </summary>
    Task<List<DailyEnergyMetric>> GetDailyMetricsAsync(DateOnly fromDate, DateOnly toDate, string? deviceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns aggregated database record counts, the latest telemetry snapshot, and system configuration.
    /// </summary>
    Task<SystemStatusDto> GetSystemStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves consolidated monthly energy consumption, generation, and grid uptime metrics.
    /// </summary>
    Task<List<MonthlyEnergyMetricDto>> GetMonthlyMetricsAsync(int? year = null, string? deviceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves 24-hour diurnal profiles for each day within the specified date range.
    /// </summary>
    Task<List<DailyHourlyMetricDto>> GetDailyHourlyMetricsAsync(DateOnly? fromDate, DateOnly? toDate, string? deviceId = null, CancellationToken cancellationToken = default);
}

