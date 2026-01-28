using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Curvia.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class Migration_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoutePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartLatitude = table.Column<double>(type: "float", nullable: false),
                    StartLongitude = table.Column<double>(type: "float", nullable: false),
                    EndLatitude = table.Column<double>(type: "float", nullable: true),
                    EndLongitude = table.Column<double>(type: "float", nullable: true),
                    IsLoop = table.Column<bool>(type: "bit", nullable: true),
                    LoopTargetDistanceMeters = table.Column<double>(type: "float", nullable: true),
                    MaxDetourRatio = table.Column<double>(type: "float", nullable: false),
                    AvoidHighways = table.Column<bool>(type: "bit", nullable: false),
                    AvoidTolls = table.Column<bool>(type: "bit", nullable: false),
                    MaxDistanceMeters = table.Column<double>(type: "float", nullable: true),
                    FunFactor = table.Column<double>(type: "float", nullable: false),
                    WeightCurves = table.Column<double>(type: "float", nullable: false),
                    WeightElevation = table.Column<double>(type: "float", nullable: false),
                    WeightScenery = table.Column<double>(type: "float", nullable: false),
                    Audit_CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Audit_CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutePlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DistanceMeters = table.Column<double>(type: "float", nullable: false),
                    DurationSeconds = table.Column<long>(type: "bigint", nullable: true),
                    ElevationGainMeters = table.Column<double>(type: "float", nullable: true),
                    FunScore = table.Column<double>(type: "float", nullable: true),
                    Geometry = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinLatitude = table.Column<double>(type: "float", nullable: false),
                    MinLongitude = table.Column<double>(type: "float", nullable: false),
                    MaxLatitude = table.Column<double>(type: "float", nullable: false),
                    MaxLongitude = table.Column<double>(type: "float", nullable: false),
                    RoutePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GraphVersionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Audit_CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Audit_CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoutePlanWaypoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    RoutePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutePlanWaypoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutePlanWaypoints_RoutePlans_RoutePlanId",
                        column: x => x.RoutePlanId,
                        principalTable: "RoutePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Geometry = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SegmentDistanceMeters = table.Column<double>(type: "float", nullable: false),
                    SegmentDurationSeconds = table.Column<long>(type: "bigint", nullable: true),
                    SegmentElevationGainMeters = table.Column<double>(type: "float", nullable: true),
                    SegmentFunScore = table.Column<double>(type: "float", nullable: true),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Audit_CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Audit_CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteSegments_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlanWaypoints_RoutePlanId",
                table: "RoutePlanWaypoints",
                column: "RoutePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteSegments_RouteId",
                table: "RouteSegments",
                column: "RouteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoutePlanWaypoints");

            migrationBuilder.DropTable(
                name: "RouteSegments");

            migrationBuilder.DropTable(
                name: "RoutePlans");

            migrationBuilder.DropTable(
                name: "Routes");
        }
    }
}
