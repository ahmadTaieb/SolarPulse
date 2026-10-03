using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Solar.Data;
using Solar.Options;

namespace Solar.Extensions;

/// <summary>
/// Extension methods for WebApplication lifecycle and startup tasks.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations automatically if configured in SolarPlatformOptions.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();
        var platformOptions = scope.ServiceProvider.GetRequiredService<IOptions<SolarPlatformOptions>>().Value;

        if (platformOptions.ApplyMigrationsOnStartup)
        {
            try
            {
                logger.LogInformation("Applying EF Core database migrations on startup...");
                var dbContext = scope.ServiceProvider.GetRequiredService<SolarDbContext>();
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("EF Core database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying database migrations on startup.");
                throw;
            }
        }
    }
}
