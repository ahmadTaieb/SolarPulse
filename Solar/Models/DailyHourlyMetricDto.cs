using System;
using System.Collections.Generic;

namespace Solar.Models;

/// <summary>
/// Represents a single day's 24-hour diurnal profile for grid power, PV generation, and load.
/// </summary>
public class DailyHourlyMetricDto
{
    public string DeviceId { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    /// <summary>
    /// Short 2-digit day number label (e.g. "06", "09", "19") as seen on hardware telemetry monitors.
    /// </summary>
    public string DayLabel => Date.Day.ToString("D2");

    /// <summary>
    /// Formatted date string (e.g. "Sep 09, 2026").
    /// </summary>
    public string FormattedDate { get; set; } = string.Empty;

    public string DayOfWeek { get; set; } = string.Empty;

    /// <summary>
    /// Exactly 24 hourly metrics from hour 0 (00:00) to hour 23 (23:00).
    /// </summary>
    public List<HourlyPointDto> Hours { get; set; } = new();

    public double PeakGridPowerWatts { get; set; }

    public double PeakPvPowerWatts { get; set; }

    public double PeakLoadPowerWatts { get; set; }

    public double TotalGridEnergyKwh { get; set; }

    public double TotalPvEnergyKwh { get; set; }

    public double TotalLoadEnergyKwh { get; set; }

    public double GridUptimeHours { get; set; }

    public double GridOutageHours { get; set; }

    public double GridAvailabilityPercentage { get; set; }
}

public class HourlyPointDto
{
    public int Hour { get; set; } // 0 - 23

    public string HourLabel => $"{Hour:D2}:00";

    public double GridPowerWatts { get; set; }

    public double PvPowerWatts { get; set; }

    public double LoadPowerWatts { get; set; }

    public bool IsGridAvailable { get; set; }
}
