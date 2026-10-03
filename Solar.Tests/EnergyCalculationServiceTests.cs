using System;
using System.Collections.Generic;
using Solar.Entities;
using Solar.Services.Calculations;
using Xunit;

namespace Solar.Tests;

public class EnergyCalculationServiceTests
{
    private readonly EnergyCalculationService _service = new();

    [Fact]
    public void CalculateDailyMetric_ConstantLoadOverOneHour_ComputesCorrectKwh()
    {
        // Arrange
        var deviceId = "dev-123";
        var date = new DateOnly(2026, 9, 23);
        var baseTime = new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

        // Two points 1 hour apart, 2000W load continuously, Grid available
        var readings = new List<InverterReading>
        {
            new()
            {
                DeviceId = deviceId,
                Timestamp = baseTime,
                LoadPowerWatts = 2000.0,
                GridPowerWatts = 1000.0,
                PvPowerWatts = 1000.0,
                AcInputVoltage = 220.0,
                IsGridAvailable = true
            },
            new()
            {
                DeviceId = deviceId,
                Timestamp = baseTime.AddHours(1),
                LoadPowerWatts = 2000.0,
                GridPowerWatts = 1000.0,
                PvPowerWatts = 1000.0,
                AcInputVoltage = 220.0,
                IsGridAvailable = true
            }
        };

        // Act
        var result = _service.CalculateDailyMetric(deviceId, date, readings);

        // Assert: 2kW * 1hr = 2.0 kWh; 1kW grid * 1hr = 1.0 kWh; 1kW PV * 1hr = 1.0 kWh
        Assert.Equal(2.0, result.TotalLoadEnergyKwh);
        Assert.Equal(1.0, result.GridImportEnergyKwh);
        Assert.Equal(1.0, result.PvGenerationEnergyKwh);
        Assert.Equal(1.0, result.GridUptimeHours);
        Assert.Equal(0.0, result.GridOutageHours);
        Assert.Equal(2, result.TelemetrySampleCount);
    }

    [Fact]
    public void CalculateDailyMetric_GridTransition_SplitsUptimeAndOutage()
    {
        // Arrange
        var deviceId = "dev-123";
        var date = new DateOnly(2026, 9, 23);
        var baseTime = new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

        // Interval 1: 30 minutes, grid on -> grid off (transition: split 15m / 15m = 0.25h / 0.25h)
        // Interval 2: 30 minutes, grid off -> grid off (full outage: 0.50h outage)
        var readings = new List<InverterReading>
        {
            new() { DeviceId = deviceId, Timestamp = baseTime, IsGridAvailable = true },
            new() { DeviceId = deviceId, Timestamp = baseTime.AddMinutes(30), IsGridAvailable = false },
            new() { DeviceId = deviceId, Timestamp = baseTime.AddMinutes(60), IsGridAvailable = false }
        };

        // Act
        var result = _service.CalculateDailyMetric(deviceId, date, readings);

        // Assert
        Assert.Equal(0.25, result.GridUptimeHours);
        Assert.Equal(0.75, result.GridOutageHours);
        Assert.Equal(3, result.TelemetrySampleCount);
    }

    [Fact]
    public void CalculateDailyMetric_SingleReading_UsesNominalFiveMinuteInterval()
    {
        // Arrange
        var deviceId = "dev-123";
        var date = new DateOnly(2026, 9, 23);
        var baseTime = new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

        var readings = new List<InverterReading>
        {
            new()
            {
                DeviceId = deviceId,
                Timestamp = baseTime,
                LoadPowerWatts = 1200.0,
                GridPowerWatts = 600.0,
                PvPowerWatts = 600.0,
                IsGridAvailable = true
            }
        };

        // Act
        var result = _service.CalculateDailyMetric(deviceId, date, readings);

        // Assert: 5 minutes = 5/60 hours = 0.0833 hours
        // 1.2 kW * (5/60) h = 0.1 kWh
        Assert.Equal(0.1, result.TotalLoadEnergyKwh);
        Assert.Equal(0.05, result.GridImportEnergyKwh);
        Assert.Equal(0.05, result.PvGenerationEnergyKwh);
        Assert.Equal(0.08, result.GridUptimeHours);
        Assert.Equal(0.0, result.GridOutageHours);
        Assert.Equal(1, result.TelemetrySampleCount);
    }
}
