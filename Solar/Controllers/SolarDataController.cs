using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Solar.Entities;
using Solar.Models;
using Solar.Services;
using Solar.Services.Telemetry;

namespace Solar.Controllers;

[ApiController]
[Route("api/solar")]
[Route("api/[controller]")]
public class SolarDataController : ControllerBase
{
    private readonly ISolarIngestionService _ingestionService;
    private readonly ISolarTelemetryService _telemetryService;

    public SolarDataController(
        ISolarIngestionService ingestionService,
        ISolarTelemetryService telemetryService)
    {
        _ingestionService = ingestionService;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Manually triggers immediate data synchronization from the cloud platform.
    /// </summary>
    [HttpPost("sync-now")]
    [ProducesResponseType(typeof(SyncResultDto), 200)]
    [ProducesResponseType(typeof(SyncResultDto), 500)]
    public async Task<IActionResult> SyncNow(CancellationToken cancellationToken)
    {
        var result = await _ingestionService.IngestDataAsync(cancellationToken);
        if (!result.Success)
        {
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Retrieves the most recent instantaneous telemetry reading.
    /// </summary>
    [HttpGet("telemetry/latest")]
    [ProducesResponseType(typeof(InverterReading), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetLatestTelemetry([FromQuery] string? deviceId, CancellationToken cancellationToken)
    {
        var reading = await _telemetryService.GetLatestReadingAsync(deviceId, cancellationToken);
        if (reading == null)
        {
            return NotFound(new { message = "No telemetry readings available yet." });
        }

        return Ok(reading);
    }

    /// <summary>
    /// Retrieves historical telemetry readings within a specified timestamp window.
    /// </summary>
    [HttpGet("telemetry/history")]
    [ProducesResponseType(typeof(List<InverterReading>), 200)]
    public async Task<IActionResult> GetTelemetryHistory(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? deviceId,
        CancellationToken cancellationToken)
    {
        DateTimeOffset toUtc = to ?? DateTimeOffset.UtcNow;
        DateTimeOffset fromUtc = from ?? toUtc.AddHours(-24);

        var readings = await _telemetryService.GetReadingsHistoryAsync(fromUtc, toUtc, deviceId, cancellationToken);
        return Ok(readings);
    }

    /// <summary>
    /// Retrieves consolidated daily energy and grid uptime metrics.
    /// </summary>
    [HttpGet("metrics/daily")]
    [ProducesResponseType(typeof(List<DailyEnergyMetric>), 200)]
    public async Task<IActionResult> GetDailyMetrics(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? deviceId,
        CancellationToken cancellationToken)
    {
        DateOnly to = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly from = fromDate ?? DateOnly.MinValue;

        var metrics = await _telemetryService.GetDailyMetricsAsync(from, to, deviceId, cancellationToken);
        return Ok(metrics);
    }

    /// <summary>
    /// Retrieves 24-hour diurnal profile bar-chart metrics for each day in range.
    /// </summary>
    [HttpGet("metrics/hourly-grid")]
    [ProducesResponseType(typeof(List<DailyHourlyMetricDto>), 200)]
    public async Task<IActionResult> GetDailyHourlyMetrics(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? deviceId,
        CancellationToken cancellationToken)
    {
        DateOnly to = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly from = fromDate ?? to.AddDays(-6);

        var metrics = await _telemetryService.GetDailyHourlyMetricsAsync(from, to, deviceId, cancellationToken);
        return Ok(metrics);
    }

    /// <summary>
    /// Retrieves consolidated monthly energy consumption, generation, and grid uptime metrics.
    /// </summary>
    [HttpGet("metrics/monthly")]
    [ProducesResponseType(typeof(List<MonthlyEnergyMetricDto>), 200)]
    public async Task<ActionResult<List<MonthlyEnergyMetricDto>>> GetMonthlyMetrics(
        [FromQuery] int? year,
        [FromQuery] string? deviceId,
        CancellationToken cancellationToken)
    {
        var metrics = await _telemetryService.GetMonthlyMetricsAsync(year, deviceId, cancellationToken);
        return Ok(metrics);
    }

    /// <summary>
    /// Returns the ingestion pipeline status, device details, and database statistics.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SystemStatusDto), 200)]
    public async Task<ActionResult<SystemStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _telemetryService.GetSystemStatusAsync(cancellationToken);
        return Ok(status);
    }
}
