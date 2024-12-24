using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class E : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacBooking_FacTimeSlot_FacTimeSlotId",
                table: "FacBooking");

            migrationBuilder.DropTable(
                name: "FacTimeSlot");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Facility");

            migrationBuilder.RenameColumn(
                name: "FacTimeSlotId",
                table: "FacBooking",
                newName: "FacilityId");

            migrationBuilder.RenameIndex(
                name: "IX_FacBooking_FacTimeSlotId",
                table: "FacBooking",
                newName: "IX_FacBooking_FacilityId");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Facility",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Facility",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "FacBooking",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "FacBooking",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "FacBooking",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_FacBooking_Facility_FacilityId",
                table: "FacBooking",
                column: "FacilityId",
                principalTable: "Facility",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacBooking_Facility_FacilityId",
                table: "FacBooking");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Facility");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "FacBooking");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "FacBooking");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "FacBooking");

            migrationBuilder.RenameColumn(
                name: "FacilityId",
                table: "FacBooking",
                newName: "FacTimeSlotId");

            migrationBuilder.RenameIndex(
                name: "IX_FacBooking_FacilityId",
                table: "FacBooking",
                newName: "IX_FacBooking_FacTimeSlotId");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Facility",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Facility",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FacTimeSlot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacilityId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacTimeSlot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacTimeSlot_Facility_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facility",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacTimeSlot_FacilityId",
                table: "FacTimeSlot",
                column: "FacilityId");

            migrationBuilder.AddForeignKey(
                name: "FK_FacBooking_FacTimeSlot_FacTimeSlotId",
                table: "FacBooking",
                column: "FacTimeSlotId",
                principalTable: "FacTimeSlot",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
