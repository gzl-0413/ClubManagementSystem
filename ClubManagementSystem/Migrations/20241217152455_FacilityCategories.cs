using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class FacilityCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FacilityCount",
                table: "FacilityCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacilityCount",
                table: "FacilityCategories");
        }
    }
}
