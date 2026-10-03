using System.ComponentModel.DataAnnotations;

namespace Solar.Options;

/// <summary>
/// Configuration options for the Solar of Things cloud platform integration.
/// </summary>
public class SolarPlatformOptions
{
    public const string SectionName = "SolarPlatform";

    /// <summary>
    /// Base URL of the Solar of Things platform.
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = "https://solar.siseli.com";

    /// <summary>
    /// User account login email or identifier.
    /// </summary>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User account password.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Ingestion frequency in minutes (defaults to 60 minutes).
    /// </summary>
    [Range(1, 1440)]
    public int SyncIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Solar Station ID (if omitted, will be auto-discovered).
    /// </summary>
    public string? StationId { get; set; }

    /// <summary>
    /// Inverter Device ID (if omitted, will be auto-discovered).
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Inverter time zone identifier (e.g. Asia/Damascus).
    /// </summary>
    public string TimeZone { get; set; } = "Asia/Damascus";

    /// <summary>
    /// Whether to automatically apply EF Core migrations on application startup.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; } = true;
}
