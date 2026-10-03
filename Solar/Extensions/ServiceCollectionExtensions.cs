using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Solar.Data;
using Solar.HostedServices;
using Solar.Options;
using Solar.Services;
using Solar.Services.Auth;
using Solar.Services.Calculations;
using Solar.Services.Parsers;
using Solar.Services.Telemetry;

namespace Solar.Extensions;

/// <summary>
/// Extension methods for configuring application dependencies and services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all database contexts, configuration options, HTTP clients, and application services.
    /// </summary>
    public static IServiceCollection AddSolarServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Entity Framework Core
        services.AddDbContext<SolarDbContext>(options =>
        {
            string connectionString = configuration.GetConnectionString("SolarDatabase")
                ?? throw new InvalidOperationException("Connection string 'SolarDatabase' was not found.");
            options.UseSqlServer(connectionString);
        });

        // 2. Strongly Typed Options
        services.AddOptions<SolarPlatformOptions>()
            .Bind(configuration.GetSection(SolarPlatformOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 3. Named HttpClient for Auth & Token Provider (Singleton)
        services.AddHttpClient("SolarPlatformAuth", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SolarPlatformOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler();

        services.AddSingleton<ISolarTokenProvider, SolarTokenProvider>();

        // 4. Typed HttpClient for SolarPlatformClient
        services.AddHttpClient<ISolarPlatformClient, SolarPlatformClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SolarPlatformOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .AddStandardResilienceHandler();

        // 5. Stateless Domain Services
        services.AddSingleton<IEnergyCalculationService, EnergyCalculationService>();
        services.AddSingleton<ISiseliDataParser, SiseliDataParser>();

        // 6. Application Scoped Services
        services.AddScoped<ISolarTelemetryService, SolarTelemetryService>();
        services.AddScoped<ISolarIngestionService, SolarIngestionService>();

        // 7. Periodic Ingestion Hosted Service
        services.AddHostedService<SolarIngestionHostedService>();

        return services;
    }
}
