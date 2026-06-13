using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrbitalWatch.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Satellites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NoradId = table.Column<string>(type: "TEXT", nullable: false),
                    OrbitalRegime = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Owner = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Satellites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConjunctionAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PrimarySatelliteId = table.Column<int>(type: "INTEGER", nullable: false),
                    SecondarySatelliteId = table.Column<int>(type: "INTEGER", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimeOfClosestApproach = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MissDistanceKm = table.Column<double>(type: "REAL", nullable: false),
                    CollisionProbability = table.Column<double>(type: "REAL", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConjunctionAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConjunctionAlerts_Satellites_PrimarySatelliteId",
                        column: x => x.PrimarySatelliteId,
                        principalTable: "Satellites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConjunctionAlerts_Satellites_SecondarySatelliteId",
                        column: x => x.SecondarySatelliteId,
                        principalTable: "Satellites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Maneuvers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SatelliteId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlannedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeltaVXKms = table.Column<double>(type: "REAL", nullable: false),
                    DeltaVYKms = table.Column<double>(type: "REAL", nullable: false),
                    DeltaVZKms = table.Column<double>(type: "REAL", nullable: false),
                    DeltaVMagnitudeKms = table.Column<double>(type: "REAL", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maneuvers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maneuvers_Satellites_SatelliteId",
                        column: x => x.SatelliteId,
                        principalTable: "Satellites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SatelliteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LatitudeDeg = table.Column<double>(type: "REAL", nullable: false),
                    LongitudeDeg = table.Column<double>(type: "REAL", nullable: false),
                    AltitudeKm = table.Column<double>(type: "REAL", nullable: false),
                    VelocityXKms = table.Column<double>(type: "REAL", nullable: false),
                    VelocityYKms = table.Column<double>(type: "REAL", nullable: false),
                    VelocityZKms = table.Column<double>(type: "REAL", nullable: false),
                    SpeedKms = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelemetryEvents_Satellites_SatelliteId",
                        column: x => x.SatelliteId,
                        principalTable: "Satellites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConjunctionAlerts_DetectedAt",
                table: "ConjunctionAlerts",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConjunctionAlerts_PrimarySatelliteId_IsResolved",
                table: "ConjunctionAlerts",
                columns: new[] { "PrimarySatelliteId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_ConjunctionAlerts_SecondarySatelliteId",
                table: "ConjunctionAlerts",
                column: "SecondarySatelliteId");

            migrationBuilder.CreateIndex(
                name: "IX_Maneuvers_SatelliteId_PlannedAt",
                table: "Maneuvers",
                columns: new[] { "SatelliteId", "PlannedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Satellites_NoradId",
                table: "Satellites",
                column: "NoradId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryEvents_SatelliteId_Timestamp",
                table: "TelemetryEvents",
                columns: new[] { "SatelliteId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryEvents_Timestamp",
                table: "TelemetryEvents",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConjunctionAlerts");

            migrationBuilder.DropTable(
                name: "Maneuvers");

            migrationBuilder.DropTable(
                name: "TelemetryEvents");

            migrationBuilder.DropTable(
                name: "Satellites");
        }
    }
}
