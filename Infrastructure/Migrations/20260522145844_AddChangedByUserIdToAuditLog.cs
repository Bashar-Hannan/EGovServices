using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGovServices.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChangedByUserIdToAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChangedByUserId",
                table: "RequestAuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestAuditLogs_ChangedByUserId",
                table: "RequestAuditLogs",
                column: "ChangedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestAuditLogs_Users_ChangedByUserId",
                table: "RequestAuditLogs",
                column: "ChangedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestAuditLogs_Users_ChangedByUserId",
                table: "RequestAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_RequestAuditLogs_ChangedByUserId",
                table: "RequestAuditLogs");

            migrationBuilder.DropColumn(
                name: "ChangedByUserId",
                table: "RequestAuditLogs");
        }
    }
}
