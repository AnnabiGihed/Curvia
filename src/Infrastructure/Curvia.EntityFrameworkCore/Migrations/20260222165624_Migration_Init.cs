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
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeycloakId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Audit_CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Audit_CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MotorcycleMakers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SuggestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotorcycleMakers", x => x.Id);
                });

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
                    LoopReturnStrategy = table.Column<int>(type: "int", nullable: true),
                    MaxDetourRatio = table.Column<double>(type: "float", nullable: false),
                    MaxDistanceMeters = table.Column<double>(type: "float", nullable: true),
                    MaxDurationSeconds = table.Column<long>(type: "bigint", nullable: true),
                    AvoidHighways = table.Column<bool>(type: "bit", nullable: false),
                    AvoidTolls = table.Column<bool>(type: "bit", nullable: false),
                    AvoidUnpaved = table.Column<bool>(type: "bit", nullable: false),
                    UrbanTolerance = table.Column<int>(type: "int", nullable: false),
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
                name: "Motorcycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Maker = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    EngineCc = table.Column<int>(type: "int", nullable: true),
                    Nickname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Audit_CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Audit_CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Motorcycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Motorcycles_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavedRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    Audit_CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Audit_CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedRoutes_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MotorcycleModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MakerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SuggestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotorcycleModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MotorcycleModels_MotorcycleMakers_MakerId",
                        column: x => x.MakerId,
                        principalTable: "MotorcycleMakers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "RouteReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SavedRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Audit_CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Audit_CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteReviews_SavedRoutes_SavedRouteId",
                        column: x => x.SavedRouteId,
                        principalTable: "SavedRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_AppUsers_Email",
                table: "AppUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AppUsers_KeycloakId",
                table: "AppUsers",
                column: "KeycloakId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MotorcycleMakers_Name",
                table: "MotorcycleMakers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_MotorcycleMakers_Status",
                table: "MotorcycleMakers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MotorcycleModels_MakerId",
                table: "MotorcycleModels",
                column: "MakerId");

            migrationBuilder.CreateIndex(
                name: "IX_MotorcycleModels_Status",
                table: "MotorcycleModels",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Motorcycles_UserId",
                table: "Motorcycles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Motorcycles_UserId_IsDefault",
                table: "Motorcycles",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlanWaypoints_RoutePlanId",
                table: "RoutePlanWaypoints",
                column: "RoutePlanId");

            migrationBuilder.CreateIndex(
                name: "UX_RouteReviews_SavedRouteId_ReviewerUserId",
                table: "RouteReviews",
                columns: new[] { "SavedRouteId", "ReviewerUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteSegments_RouteId",
                table: "RouteSegments",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedRoutes_RouteId",
                table: "SavedRoutes",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedRoutes_Visibility",
                table: "SavedRoutes",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "UX_SavedRoutes_UserId_RouteId",
                table: "SavedRoutes",
                columns: new[] { "UserId", "RouteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MotorcycleModels");

            migrationBuilder.DropTable(
                name: "Motorcycles");

            migrationBuilder.DropTable(
                name: "RoutePlanWaypoints");

            migrationBuilder.DropTable(
                name: "RouteReviews");

            migrationBuilder.DropTable(
                name: "RouteSegments");

            migrationBuilder.DropTable(
                name: "MotorcycleMakers");

            migrationBuilder.DropTable(
                name: "RoutePlans");

            migrationBuilder.DropTable(
                name: "SavedRoutes");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "AppUsers");
        }
    }
}
