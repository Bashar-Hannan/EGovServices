using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGovServices.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrafficEntryFeild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CitizenNationalNumber",
                table: "TrafficViolations",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CameraId",
                table: "TrafficViolations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuedByOfficerName",
                table: "TrafficViolations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "TrafficViolations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrafficViolations_CitizenNationalNumber",
                table: "TrafficViolations",
                column: "CitizenNationalNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_TrafficViolations_Citizens_CitizenNationalNumber",
                table: "TrafficViolations",
                column: "CitizenNationalNumber",
                principalTable: "Citizens",
                principalColumn: "NationalNumber",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrafficViolations_Citizens_CitizenNationalNumber",
                table: "TrafficViolations");

            migrationBuilder.DropIndex(
                name: "IX_TrafficViolations_CitizenNationalNumber",
                table: "TrafficViolations");

            migrationBuilder.DropColumn(
                name: "CameraId",
                table: "TrafficViolations");

            migrationBuilder.DropColumn(
                name: "IssuedByOfficerName",
                table: "TrafficViolations");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "TrafficViolations");

            migrationBuilder.AlterColumn<string>(
                name: "CitizenNationalNumber",
                table: "TrafficViolations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");
        }
    }
}
