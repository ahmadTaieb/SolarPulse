using System;
using System.Collections.Generic;
using Solar.Entities;

namespace Solar.Services.Calculations;

/// <summary>
/// Domain service implementing trapezoidal numerical integration for solar energy and grid availability metrics.
/// </summary>
public class EnergyCalculationService : IEnergyCalculationService
{
    private const double NominalIntervalHours = 5.0 / 60.0; // 5-minute nominal sample
    private const double MaxReasonableIntervalHours = 1.0;

    public DailyEnergyMetric CalculateDailyMetric(string deviceId, DateOnly date, IReadOnlyList<InverterReading> readings)
    {
        double totalLoadEnergyKwh = 0.0;
        double gridImportEnergyKwh = 0.0;
        double pvGenerationEnergyKwh = 0.0;
        double gridUptimeHours = 0.0;
        double gridOutageHours = 0.0;

        if (readings.Count > 1)
        {
            for (int i = 1; i < readings.Count; i++)
            {
                var prev = readings[i - 1];
                var curr = readings[i];

                double intervalHours = (curr.Timestamp - prev.Timestamp).TotalHours;

                // Protect against negative intervals or extended gaps (e.g. system shut down for days)
                if (intervalHours <= 0 || intervalHours > MaxReasonableIntervalHours)
                {
                    intervalHours = NominalIntervalHours;
                }

                // 1. Grid Uptime Duration
                if (curr.IsGridAvailable && prev.IsGridAvailable)
                {
                    gridUptimeHours += intervalHours;
                }
                else if (!curr.IsGridAvailable && !prev.IsGridAvailable)
                {
                    gridOutageHours += intervalHours;
                }
                else
                {
                    // State transitioned during this interval: split 50/50
                    gridUptimeHours += intervalHours / 2.0;
                    gridOutageHours += intervalHours / 2.0;
                }

                // 2. Numerical Integration for Energy (Trapezoidal Rule: kWh = Avg kW * Hours)
                double avgLoadKw = ((prev.LoadPowerWatts + curr.LoadPowerWatts) / 2.0) / 1000.0;
                totalLoadEnergyKwh += avgLoadKw * intervalHours;

                double avgGridKw = ((prev.GridPowerWatts + curr.GridPowerWatts) / 2.0) / 1000.0;
                gridImportEnergyKwh += avgGridKw * intervalHours;

                double avgPvKw = ((prev.PvPowerWatts + curr.PvPowerWatts) / 2.0) / 1000.0;
                pvGenerationEnergyKwh += avgPvKw * intervalHours;
            }
        }
        else if (readings.Count == 1)
        {
            var single = readings[0];
            double intervalHours = NominalIntervalHours;

            if (single.IsGridAvailable)
            {
                gridUptimeHours = intervalHours;
            }
            else
            {
                gridOutageHours = intervalHours;
            }

            totalLoadEnergyKwh = (single.LoadPowerWatts / 1000.0) * intervalHours;
            gridImportEnergyKwh = (single.GridPowerWatts / 1000.0) * intervalHours;
            pvGenerationEnergyKwh = (single.PvPowerWatts / 1000.0) * intervalHours;
        }

        return new DailyEnergyMetric
        {
            DeviceId = deviceId,
            Date = date,
            TotalLoadEnergyKwh = Math.Round(totalLoadEnergyKwh, 4),
            GridImportEnergyKwh = Math.Round(gridImportEnergyKwh, 4),
            PvGenerationEnergyKwh = Math.Round(pvGenerationEnergyKwh, 4),
            GridUptimeHours = Math.Round(gridUptimeHours, 2),
            GridOutageHours = Math.Round(gridOutageHours, 2),
            TelemetrySampleCount = readings.Count,
            LastCalculatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
