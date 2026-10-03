using Microsoft.EntityFrameworkCore;
using Solar.Entities;

namespace Solar.Data;

public class SolarDbContext : DbContext
{
    public SolarDbContext(DbContextOptions<SolarDbContext> options) : base(options)
    {
    }

    public DbSet<InverterReading> InverterReadings => Set<InverterReading>();
    public DbSet<DailyEnergyMetric> DailyEnergyMetrics => Set<DailyEnergyMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InverterReading>(entity =>
        {
            entity.ToTable("InverterReadings");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.DeviceId)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.OperatingMode)
                .HasMaxLength(32);

            entity.Property(e => e.Timestamp)
                .IsRequired();

            // Create composite unique index on DeviceId + Timestamp to prevent duplicate telemetry snapshots
            entity.HasIndex(e => new { e.DeviceId, e.Timestamp })
                .IsUnique()
                .HasDatabaseName("IX_InverterReadings_DeviceId_Timestamp");

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_InverterReadings_Timestamp");
        });

        modelBuilder.Entity<DailyEnergyMetric>(entity =>
        {
            entity.ToTable("DailyEnergyMetrics");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.DeviceId)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.Date)
                .IsRequired();

            // Unique index to ensure one consolidated record per device per day
            entity.HasIndex(e => new { e.DeviceId, e.Date })
                .IsUnique()
                .HasDatabaseName("IX_DailyEnergyMetrics_DeviceId_Date");
        });
    }
}
