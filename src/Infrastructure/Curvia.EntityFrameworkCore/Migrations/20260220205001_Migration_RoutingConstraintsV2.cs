using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Curvia.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class Migration_RoutingConstraintsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AvoidUnpaved",
                table: "RoutePlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LoopReturnStrategy",
                table: "RoutePlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MaxDurationSeconds",
                table: "RoutePlans",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UrbanTolerance",
                table: "RoutePlans",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvoidUnpaved",
                table: "RoutePlans");

            migrationBuilder.DropColumn(
                name: "LoopReturnStrategy",
                table: "RoutePlans");

            migrationBuilder.DropColumn(
                name: "MaxDurationSeconds",
                table: "RoutePlans");

            migrationBuilder.DropColumn(
                name: "UrbanTolerance",
                table: "RoutePlans");
        }
    }
}
