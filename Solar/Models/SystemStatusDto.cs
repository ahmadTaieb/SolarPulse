using System;

namespace Solar.Models;

/// <summary>
/// Diagnostics and status payload describing the ingestion system, device configuration, and database totals.
/// </summary>
public class SystemStatusDto
{
    public string Status { get; set; } = "Operational";
    public int SyncIntervalMinutes { get; set; }
    public string? ConfiguredStationId { get; set; }
    public string? ConfiguredDeviceId { get; set; }
    public string? ConfiguredTimeZone { get; set; }
    public DatabaseStatusDto Database { get; set; } = new();
    public DateTimeOffset ServerTimeUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class DatabaseStatusDto
{
    public long TotalInverterReadings { get; set; }
    public long TotalDailyMetricRecords { get; set; }
    public DateTimeOffset? LatestReadingTimestampUtc { get; set; }
    public double? LatestAcInputVoltage { get; set; }
    public double? LatestGridPowerWatts { get; set; }
    public double? LatestLoadPowerWatts { get; set; }
    public bool? IsGridAvailable { get; set; }
    public double? BatterySoc { get; set; }
}
