using System.Collections.Generic;
using Solar.Models.Siseli;
using Solar.Services.Parsers;
using Xunit;

namespace Solar.Tests;

public class SiseliDataParserTests
{
    private readonly SiseliDataParser _parser = new();

    [Fact]
    public void ParseTimeSeriesReadings_ValidPayload_MapsUnitsCorrectly()
    {
        // Arrange
        var deviceId = "dev-test";
        var payload = new TimeSeriesPayload
        {
            TimeSeries = ["2026-09-23T08:00:00Z", "2026-09-23T08:05:00Z"],
            Fields = new Dictionary<string, List<double?>>
            {
                ["pvPower"] = [1.5, 2.0],              // in kW
                ["outputActivePower"] = [0.8, 1.2],    // in kW
                ["gridPower"] = [0.0, 0.5],            // in kW
                ["acInputVoltage"] = [0.0, 225.0],     // in Volts
                ["batterySOC"] = [95.0, 96.0],
                ["batteryVoltage"] = [52.4, 52.8]
            }
        };

        // Act
        var readings = _parser.ParseTimeSeriesReadings(deviceId, payload);

        // Assert
        Assert.Equal(2, readings.Count);

        var first = readings[0];
        Assert.Equal(deviceId, first.DeviceId);
        Assert.Equal(1500.0, first.PvPowerWatts);
        Assert.Equal(800.0, first.LoadPowerWatts);
        Assert.Equal(0.0, first.GridPowerWatts);
        Assert.Equal(0.0, first.AcInputVoltage);
        Assert.False(first.IsGridAvailable);
        Assert.Equal(95.0, first.BatterySoc);
        Assert.Equal(52.4, first.BatteryVoltage);

        var second = readings[1];
        Assert.Equal(2000.0, second.PvPowerWatts);
        Assert.Equal(1200.0, second.LoadPowerWatts);
        Assert.Equal(500.0, second.GridPowerWatts);
        Assert.Equal(225.0, second.AcInputVoltage);
        Assert.True(second.IsGridAvailable); // > 50V
        Assert.Equal(96.0, second.BatterySoc);
        Assert.Equal(52.8, second.BatteryVoltage);
    }

    [Fact]
    public void ParseEnergyFlowReading_WithCoercedTypes_ParsesProperly()
    {
        // Arrange
        var deviceId = "dev-live";
        var state = new EnergyFlowDeviceState
        {
            Time = "2026-09-23T09:30:00Z",
            Fields = new Dictionary<string, EnergyFlowFieldItem>
            {
                ["generationPower"] = new() { Key = "generationPower", Value = "3.45" }, // string number
                ["loadPower"] = new() { Key = "loadPower", Value = 1.2 },               // double
                ["gridVoltage"] = new() { Key = "gridVoltage", Value = 230 },           // int
                ["batterySOC"] = new() { Key = "batterySOC", Value = 88 }
            }
        };

        // Act
        var reading = _parser.ParseEnergyFlowReading(deviceId, state);

        // Assert
        Assert.NotNull(reading);
        Assert.Equal(deviceId, reading.DeviceId);
        Assert.Equal(3450.0, reading.PvPowerWatts);
        Assert.Equal(1200.0, reading.LoadPowerWatts);
        Assert.Equal(230.0, reading.AcInputVoltage);
        Assert.True(reading.IsGridAvailable);
        Assert.Equal(88.0, reading.BatterySoc);
    }
}
