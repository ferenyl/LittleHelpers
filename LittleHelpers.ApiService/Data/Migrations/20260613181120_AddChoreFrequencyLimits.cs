using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LittleHelpers.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChoreFrequencyLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxTimesPerDay",
                table: "Chores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxTimesPerWeek",
                table: "Chores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinDaysBetween",
                table: "Chores",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxTimesPerDay",
                table: "Chores");

            migrationBuilder.DropColumn(
                name: "MaxTimesPerWeek",
                table: "Chores");

            migrationBuilder.DropColumn(
                name: "MinDaysBetween",
                table: "Chores");
        }
    }
}
