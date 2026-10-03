using System;
using Solar.Entities;

namespace Solar.Models;

public class SyncResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public DateTimeOffset SyncTimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    public int TelemetryReadingsIngested { get; set; }
    public InverterReading? LatestReading { get; set; }
    public DailyEnergyMetric? TodayMetric { get; set; }
    public TimeSpan Duration { get; set; }
}
