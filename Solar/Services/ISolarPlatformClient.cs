using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Solar.Models.Siseli;

namespace Solar.Services;

/// <summary>
/// Client contract for querying the Solar of Things (Siseli) cloud platform.
/// </summary>
public interface ISolarPlatformClient
{
    /// <summary>
    /// Retrieves all solar stations owned by the authenticated account.
    /// </summary>
    Task<List<StationItem>> GetStationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all inverter devices under the specified station (or all stations if omitted).
    /// </summary>
    Task<List<DeviceItem>> GetDevicesAsync(string? stationId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches historical time-series intervals for an inverter.
    /// </summary>
    Task<TimeSeriesPayload?> GetTimeSeriesDataAsync(string deviceId, DateTimeOffset fromTime, DateTimeOffset toTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the live energy flow snapshot for an inverter.
    /// </summary>
    Task<EnergyFlowDeviceState?> GetLatestEnergyFlowAsync(string deviceId, CancellationToken cancellationToken = default);
}
