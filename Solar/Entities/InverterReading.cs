using System;

namespace Solar.Entities;

/// <summary>
/// Represents an instantaneous or periodic telemetry snapshot from the solar inverter.
/// </summary>
public class InverterReading
{
    public long Id { get; set; }

    /// <summary>
    /// Identifier of the inverter device.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Exact timestamp reported by the inverter telemetry stream (UTC).
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Power drawn exclusively from the public utility grid in Watts (W).
    /// </summary>
    public double GridPowerWatts { get; set; }

    /// <summary>
    /// Total electricity consumed by household loads in Watts (W).
    /// </summary>
    public double LoadPowerWatts { get; set; }

    /// <summary>
    /// Solar photovoltaic power generation in Watts (W).
    /// </summary>
    public double PvPowerWatts { get; set; }

    /// <summary>
    /// Utility grid AC input voltage in Volts (V).
    /// </summary>
    public double AcInputVoltage { get; set; }

    /// <summary>
    /// Indicates whether the public utility grid was actively available and supplying power.
    /// </summary>
    public bool IsGridAvailable { get; set; }

    /// <summary>
    /// Battery State of Charge percentage (0-100%).
    /// </summary>
    public double? BatterySoc { get; set; }

    /// <summary>
    /// Battery terminal voltage in Volts (V).
    /// </summary>
    public double? BatteryVoltage { get; set; }

    /// <summary>
    /// Inverter operating mode (e.g. SUB, SBU, Mains, Battery).
    /// </summary>
    public string? OperatingMode { get; set; }

    /// <summary>
    /// Timestamp when this record was ingested into the local database (UTC).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
