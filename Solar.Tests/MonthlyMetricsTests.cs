using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Solar.Data;
using Solar.Entities;
using Solar.Options;
using Solar.Services.Telemetry;
using Xunit;

namespace Solar.Tests;

public class MonthlyMetricsTests
{
    private static SolarDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SolarDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SolarDbContext(options);
    }

    [Fact]
    public async Task GetMonthlyMetricsAsync_AggregatesDaysWithinSameMonth()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var platformOptions = Microsoft.Extensions.Options.Options.Create(new SolarPlatformOptions());
        var service = new SolarTelemetryService(context, platformOptions);

        var deviceId = "dev-test";

        context.DailyEnergyMetrics.AddRange(
            new DailyEnergyMetric
            {
                DeviceId = deviceId,
                Date = new DateOnly(2026, 9, 1),
                TotalLoadEnergyKwh = 10.0,
                GridImportEnergyKwh = 4.0,
                PvGenerationEnergyKwh = 6.0,
                GridUptimeHours = 12.0,
                GridOutageHours = 12.0,
                TelemetrySampleCount = 100
            },
            new DailyEnergyMetric
            {
                DeviceId = deviceId,
                Date = new DateOnly(2026, 9, 2),
                TotalLoadEnergyKwh = 12.0,
                GridImportEnergyKwh = 2.0,
                PvGenerationEnergyKwh = 10.0,
                GridUptimeHours = 18.0,
                GridOutageHours = 6.0,
                TelemetrySampleCount = 100
            },
            new DailyEnergyMetric
            {
                DeviceId = deviceId,
                Date = new DateOnly(2026, 8, 15), // Different month
                TotalLoadEnergyKwh = 8.0,
                GridImportEnergyKwh = 3.0,
                PvGenerationEnergyKwh = 5.0,
                GridUptimeHours = 20.0,
                GridOutageHours = 4.0,
                TelemetrySampleCount = 80
            }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetMonthlyMetricsAsync(year: 2026, deviceId: deviceId);

        // Assert
        Assert.Equal(2, result.Count); // August and September

        var sept = result.Find(m => m.Month == 9);
        Assert.NotNull(sept);
        Assert.Equal("September 2026", sept.MonthName);
        Assert.Equal(22.0, sept.TotalLoadEnergyKwh); // 10 + 12
        Assert.Equal(6.0, sept.GridImportEnergyKwh);  // 4 + 2
        Assert.Equal(16.0, sept.PvGenerationEnergyKwh); // 6 + 10
        Assert.Equal(30.0, sept.GridUptimeHours); // 12 + 18
        Assert.Equal(18.0, sept.GridOutageHours); // 12 + 6
        Assert.Equal(62.5, sept.GridAvailabilityPercentage); // 30 / (30 + 18) * 100 = 62.5%
        Assert.Equal(2, sept.DaysRecorded);
        Assert.Equal(11.0, sept.AverageDailyLoadKwh); // 22 / 2
        Assert.Equal(8.0, sept.AverageDailyPvKwh); // 16 / 2
    }

    [Fact]
    public async Task GetMonthlyMetricsAsync_FilterByYear_ExcludesOtherYears()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var platformOptions = Microsoft.Extensions.Options.Options.Create(new SolarPlatformOptions());
        var service = new SolarTelemetryService(context, platformOptions);

        var deviceId = "dev-year-filter";

        context.DailyEnergyMetrics.AddRange(
            new DailyEnergyMetric
            {
                DeviceId = deviceId,
                Date = new DateOnly(2025, 9, 1),
                TotalLoadEnergyKwh = 5.0
            },
            new DailyEnergyMetric
            {
                DeviceId = deviceId,
                Date = new DateOnly(2026, 9, 1),
                TotalLoadEnergyKwh = 15.0
            }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetMonthlyMetricsAsync(year: 2026, deviceId: deviceId);

        // Assert
        Assert.Single(result);
        Assert.Equal(2026, result[0].Year);
        Assert.Equal(15.0, result[0].TotalLoadEnergyKwh);
    }
}
