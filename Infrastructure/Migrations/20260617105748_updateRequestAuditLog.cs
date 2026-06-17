using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGovServices.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateRequestAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RequestAuditLogs_RequestId_CreatedAt",
                table: "RequestAuditLogs");

            migrationBuilder.AlterColumn<string>(
                name: "NewStatus",
                table: "RequestAuditLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "RequestAuditLogs",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(7)");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "RequestAuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "RequestAuditLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldStatus",
                table: "RequestAuditLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestAuditLogs_ServiceRequestId",
                table: "RequestAuditLogs",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestAuditLogs_ServiceRequestId_CreatedAt",
                table: "RequestAuditLogs",
                columns: new[] { "ServiceRequestId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RequestAuditLogs_ServiceRequestId",
                table: "RequestAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_RequestAuditLogs_ServiceRequestId_CreatedAt",
                table: "RequestAuditLogs");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "RequestAuditLogs");

            migrationBuilder.DropColumn(
                name: "OldStatus",
                table: "RequestAuditLogs");

            migrationBuilder.AlterColumn<string>(
                name: "NewStatus",
                table: "RequestAuditLogs",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "RequestAuditLogs",
                type: "datetime2(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "RequestAuditLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_RequestAuditLogs_RequestId_CreatedAt",
                table: "RequestAuditLogs",
                columns: new[] { "ServiceRequestId", "CreatedAt" },
                descending: new[] { false, true });
        }
    }
}
