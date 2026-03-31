using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LittleHelpers.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowanceToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyAllowance",
                table: "Users",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PointsGoal",
                table: "Users",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyAllowance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PointsGoal",
                table: "Users");
        }
    }
}
