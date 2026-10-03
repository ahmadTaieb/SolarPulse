using System;
using System.Collections.Generic;
using Solar.Entities;

namespace Solar.Services.Calculations;

/// <summary>
/// Service contract for computing consolidated energy generation, consumption, and grid reliability metrics.
/// </summary>
public interface IEnergyCalculationService
{
    /// <summary>
    /// Computes daily energy consumption, generation, and grid uptime/outage metrics using trapezoidal numerical integration.
    /// </summary>
    /// <param name="deviceId">Inverter device identifier.</param>
    /// <param name="date">Calendar date for the metric.</param>
    /// <param name="readings">Chronologically sorted telemetry readings for the target calendar day.</param>
    DailyEnergyMetric CalculateDailyMetric(string deviceId, DateOnly date, IReadOnlyList<InverterReading> readings);
}
