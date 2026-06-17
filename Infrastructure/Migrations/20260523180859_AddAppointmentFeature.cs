using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGovServices.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentSlotId",
                table: "ServiceRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ServiceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ServiceRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "GovernmentServices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ServiceType",
                table: "GovernmentServices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AppointmentSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GovernmentServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    TotalSeats = table.Column<int>(type: "int", nullable: false),
                    BookedSeats = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentSlots_GovernmentServices_GovernmentServiceId",
                        column: x => x.GovernmentServiceId,
                        principalTable: "GovernmentServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentSlots_Users_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "GovernmentServices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "ServiceType" },
                values: new object[] { new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_AppointmentSlotId",
                table: "ServiceRequests",
                column: "AppointmentSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSlots_CreatedByAdminId",
                table: "AppointmentSlots",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSlots_GovernmentServiceId",
                table: "AppointmentSlots",
                column: "GovernmentServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequests_AppointmentSlots_AppointmentSlotId",
                table: "ServiceRequests",
                column: "AppointmentSlotId",
                principalTable: "AppointmentSlots",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequests_AppointmentSlots_AppointmentSlotId",
                table: "ServiceRequests");

            migrationBuilder.DropTable(
                name: "AppointmentSlots");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_AppointmentSlotId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "AppointmentSlotId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "GovernmentServices");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "GovernmentServices");
        }
    }
}
