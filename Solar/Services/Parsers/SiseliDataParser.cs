using System;
using System.Collections.Generic;
using System.Globalization;
using Solar.Entities;
using Solar.Models.Siseli;

namespace Solar.Services.Parsers;

/// <summary>
/// Handles translation, unit scaling, and numeric coercion from raw Siseli API JSON payloads to InverterReading entities.
/// </summary>
public class SiseliDataParser : ISiseliDataParser
{
    private const double GridVoltageThresholdVolts = 50.0;

    public List<InverterReading> ParseTimeSeriesReadings(string deviceId, TimeSeriesPayload payload)
    {
        var result = new List<InverterReading>();
        var timestamps = payload.TimeSeries;
        var fields = payload.Fields;

        for (int i = 0; i < timestamps.Count; i++)
        {
            if (!DateTimeOffset.TryParse(timestamps[i], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
            {
                continue;
            }

            // Power in API is typically returned in kW (e.g. 0.447 kW) -> convert to Watts
            double pvKw = GetFieldValue(fields, i, "pvPower", "pvInputPower");
            double loadKw = GetFieldValue(fields, i, "outputActivePower", "acOutputActivePower");
            double gridKw = GetFieldValue(fields, i, "mainsPower", "gridPower");
            double acVoltage = GetFieldValue(fields, i, "acInputVoltage", "gridVoltage");
            double? batterySoc = GetNullableFieldValue(fields, i, "batteryCapacity", "batterySOC");
            double? batteryVolt = GetNullableFieldValue(fields, i, "batteryVoltage");

            result.Add(new InverterReading
            {
                DeviceId = deviceId,
                Timestamp = dt.ToUniversalTime(),
                PvPowerWatts = Math.Max(0.0, pvKw * 1000.0),
                LoadPowerWatts = Math.Max(0.0, loadKw * 1000.0),
                GridPowerWatts = Math.Max(0.0, gridKw * 1000.0),
                AcInputVoltage = acVoltage,
                IsGridAvailable = acVoltage > GridVoltageThresholdVolts,
                BatterySoc = batterySoc,
                BatteryVoltage = batteryVolt,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        return result;
    }

    public InverterReading? ParseEnergyFlowReading(string deviceId, EnergyFlowDeviceState state)
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(state.Time) &&
            DateTimeOffset.TryParse(state.Time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedTime))
        {
            timestamp = parsedTime.ToUniversalTime();
        }

        var fields = state.Fields;
        double pvKw = CoerceNumber(fields.GetValueOrDefault("pvPower")?.Value) ??
                      CoerceNumber(fields.GetValueOrDefault("generationPower")?.Value) ?? 0.0;
        double loadKw = CoerceNumber(fields.GetValueOrDefault("outputActivePower")?.Value) ??
                        CoerceNumber(fields.GetValueOrDefault("loadPower")?.Value) ?? 0.0;
        double gridKw = CoerceNumber(fields.GetValueOrDefault("mainsPower")?.Value) ??
                        CoerceNumber(fields.GetValueOrDefault("gridPower")?.Value) ?? 0.0;
        double acVoltage = CoerceNumber(fields.GetValueOrDefault("acInputVoltage")?.Value) ??
                           CoerceNumber(fields.GetValueOrDefault("gridVoltage")?.Value) ?? 0.0;
        double? batterySoc = CoerceNumber(fields.GetValueOrDefault("batteryCapacity")?.Value) ??
                             CoerceNumber(fields.GetValueOrDefault("batterySOC")?.Value);
        double? batteryVolt = CoerceNumber(fields.GetValueOrDefault("batteryVoltage")?.Value);

        return new InverterReading
        {
            DeviceId = deviceId,
            Timestamp = timestamp,
            PvPowerWatts = Math.Max(0.0, pvKw * 1000.0),
            LoadPowerWatts = Math.Max(0.0, loadKw * 1000.0),
            GridPowerWatts = Math.Max(0.0, gridKw * 1000.0),
            AcInputVoltage = acVoltage,
            IsGridAvailable = acVoltage > GridVoltageThresholdVolts,
            BatterySoc = batterySoc,
            BatteryVoltage = batteryVolt,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static double GetFieldValue(Dictionary<string, List<double?>> fields, int index, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (fields.TryGetValue(key, out var list) && index < list.Count && list[index].HasValue)
            {
                return list[index]!.Value;
            }
        }
        return 0.0;
    }

    private static double? GetNullableFieldValue(Dictionary<string, List<double?>> fields, int index, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (fields.TryGetValue(key, out var list) && index < list.Count && list[index].HasValue)
            {
                return list[index];
            }
        }
        return null;
    }

    private static double? CoerceNumber(object? value)
    {
        if (value == null) return null;
        if (value is double d) return d;
        if (value is int i) return i;
        if (value is long l) return l;
        if (value is float f) return f;
        if (value is decimal m) return (double)m;
        if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        return null;
    }
}
