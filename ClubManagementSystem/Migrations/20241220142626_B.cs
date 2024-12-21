using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class B : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacilityCategoryId",
                table: "Facility");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FacilityCategoryId",
                table: "Facility",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
