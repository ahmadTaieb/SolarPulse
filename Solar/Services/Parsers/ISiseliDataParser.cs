using System.Collections.Generic;
using Solar.Entities;
using Solar.Models.Siseli;

namespace Solar.Services.Parsers;

/// <summary>
/// Parser contract for converting raw Siseli platform API payloads into domain InverterReading entities.
/// </summary>
public interface ISiseliDataParser
{
    /// <summary>
    /// Parses historical time-series payload arrays and field maps into normalized InverterReading entities.
    /// </summary>
    List<InverterReading> ParseTimeSeriesReadings(string deviceId, TimeSeriesPayload payload);

    /// <summary>
    /// Parses instantaneous energy flow snapshot payload into a normalized InverterReading entity.
    /// </summary>
    InverterReading? ParseEnergyFlowReading(string deviceId, EnergyFlowDeviceState state);
}
