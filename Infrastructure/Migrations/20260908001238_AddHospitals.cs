using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGovServices.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hospitals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    GovernmentEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Governorate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Specialty = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    InfrastructureStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OperationalStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BedsActual = table.Column<int>(type: "int", nullable: true),
                    BedsTheoretical = table.Column<int>(type: "int", nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    AdminPhoneNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    DirectorPhoneNumber = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hospitals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hospitals_GovernmentEntities_GovernmentEntityId",
                        column: x => x.GovernmentEntityId,
                        principalTable: "GovernmentEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hospitals_EntityId_Governorate_Active",
                table: "Hospitals",
                columns: new[] { "GovernmentEntityId", "Governorate", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Hospitals_Specialty",
                table: "Hospitals",
                column: "Specialty");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hospitals");
        }
    }
}
