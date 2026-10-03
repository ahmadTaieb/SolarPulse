using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solar.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyEnergyMetrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalLoadEnergyKwh = table.Column<double>(type: "float", nullable: false),
                    GridImportEnergyKwh = table.Column<double>(type: "float", nullable: false),
                    PvGenerationEnergyKwh = table.Column<double>(type: "float", nullable: false),
                    GridUptimeHours = table.Column<double>(type: "float", nullable: false),
                    GridOutageHours = table.Column<double>(type: "float", nullable: false),
                    TelemetrySampleCount = table.Column<int>(type: "int", nullable: false),
                    LastCalculatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyEnergyMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InverterReadings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GridPowerWatts = table.Column<double>(type: "float", nullable: false),
                    LoadPowerWatts = table.Column<double>(type: "float", nullable: false),
                    PvPowerWatts = table.Column<double>(type: "float", nullable: false),
                    AcInputVoltage = table.Column<double>(type: "float", nullable: false),
                    IsGridAvailable = table.Column<bool>(type: "bit", nullable: false),
                    BatterySoc = table.Column<double>(type: "float", nullable: true),
                    BatteryVoltage = table.Column<double>(type: "float", nullable: true),
                    OperatingMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InverterReadings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyEnergyMetrics_DeviceId_Date",
                table: "DailyEnergyMetrics",
                columns: new[] { "DeviceId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InverterReadings_DeviceId_Timestamp",
                table: "InverterReadings",
                columns: new[] { "DeviceId", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InverterReadings_Timestamp",
                table: "InverterReadings",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyEnergyMetrics");

            migrationBuilder.DropTable(
                name: "InverterReadings");
        }
    }
}
