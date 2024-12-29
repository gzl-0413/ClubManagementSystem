using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class i : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
    name: "Feedbacks",
    columns: table => new
    {
        Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
        Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
        Photo = table.Column<string>(type: "nvarchar(max)", nullable: false),
        ReadStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
        CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
        ReplyContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
        ReplyPhoto = table.Column<string>(type: "nvarchar(max)", nullable: false),
        ReplyDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
        ReplyStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
        AdminEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
        UserEmail = table.Column<string>(type: "nvarchar(max)", nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_Feedbacks", x => x.Id);
    });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
    name: "Feedback");
        }
    }
}
