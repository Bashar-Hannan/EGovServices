using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EGovServices.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class @new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Citizens",
                columns: table => new
                {
                    NationalNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FatherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MotherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Email = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citizens", x => x.NationalNumber);
                });

            migrationBuilder.CreateTable(
                name: "FAQ",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Question = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FAQ", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GovernmentEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernmentEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OtpVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    NationalNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    OtpCode = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TempPhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TempEmail = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    TempPasswordHash = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpVerifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CitizenPhones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CitizenNationalNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenPhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CitizenPhones_Citizens_CitizenNationalNumber",
                        column: x => x.CitizenNationalNumber,
                        principalTable: "Citizens",
                        principalColumn: "NationalNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CriminalRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    CitizenNationalNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CrimeDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    JudgmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriminalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CriminalRecords_Citizens_CitizenNationalNumber",
                        column: x => x.CitizenNationalNumber,
                        principalTable: "Citizens",
                        principalColumn: "NationalNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    NationalNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Role = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Citizens_NationalNumber",
                        column: x => x.NationalNumber,
                        principalTable: "Citizens",
                        principalColumn: "NationalNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    GovernmentEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_GovernmentEntities_GovernmentEntityId",
                        column: x => x.GovernmentEntityId,
                        principalTable: "GovernmentEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GovernmentServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    GovernmentEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Requirements = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ServiceFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernmentServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GovernmentServices_GovernmentEntities_GovernmentEntityId",
                        column: x => x.GovernmentEntityId,
                        principalTable: "GovernmentEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NotificationType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Currency = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wallets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceFormFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    GovernmentServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FieldType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HelpText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceFormFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceFormFields_GovernmentServices_GovernmentServiceId",
                        column: x => x.GovernmentServiceId,
                        principalTable: "GovernmentServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GovernmentServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FormData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessingNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRequests_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceRequests_GovernmentServices_GovernmentServiceId",
                        column: x => x.GovernmentServiceId,
                        principalTable: "GovernmentServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    GovernmentServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceSlots_GovernmentServices_GovernmentServiceId",
                        column: x => x.GovernmentServiceId,
                        principalTable: "GovernmentServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceFieldOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ServiceFormFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionValue = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    OptionLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceFieldOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceFieldOptions_ServiceFormFields_ServiceFormFieldId",
                        column: x => x.ServiceFormFieldId,
                        principalTable: "ServiceFormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ServiceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FileType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ServiceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestAuditLogs_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferenceId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ServiceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_ServiceSlots_ServiceSlotId",
                        column: x => x.ServiceSlotId,
                        principalTable: "ServiceSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Citizens",
                columns: new[] { "NationalNumber", "Address", "BirthDate", "Email", "FatherName", "FirstName", "Gender", "LastName", "MotherName" },
                values: new object[,]
                {
                    { "01100000003", "اللاذقية - المشروع الأول", new DateOnly(2000, 1, 15), "yassin@example.com", "عمر", "ياسين", "ذكر", "الكردي", "ليلى" },
                    { "01200000002", "دمشق - المزة", new DateOnly(1998, 11, 22), "sara@example.com", "محمود", "سارة", "أنثى", "الأحمد", "مريم" },
                    { "02100000001", "حلب - حي الفرقان", new DateOnly(1995, 5, 10), "ahmed@example.com", "محمد", "أحمد", "ذكر", "المنصور", "فاطمة" },
                    { "02250150972", "حلب - الشيخ مقصود", new DateOnly(2002, 12, 29), "basharhannan400@gmail.com", "عماد", "بشار", "ذكر", "حنان", "فريدة" }
                });

            migrationBuilder.InsertData(
                table: "GovernmentEntities",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "Ministry of Interior", true, "وزارة الداخلية" });

            migrationBuilder.InsertData(
                table: "GovernmentServices",
                columns: new[] { "Id", "Description", "GovernmentEntityId", "IsActive", "Name", "Requirements", "ServiceFee" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), "خدمة تجديد جواز السفر منتهي الصلاحية", new Guid("11111111-1111-1111-1111-111111111111"), true, "تجديد جواز السفر", "صورة شخصية + جواز السفر القديم + إثبات السكن", 150.00m });

            migrationBuilder.InsertData(
                table: "ServiceFormFields",
                columns: new[] { "Id", "DefaultValue", "DisplayOrder", "FieldName", "FieldType", "GovernmentServiceId", "HelpText", "IsActive", "IsRequired", "Label", "Metadata", "Placeholder", "ValidationRules" },
                values: new object[,]
                {
                    { new Guid("33333333-0001-0001-0001-000000000001"), null, 1, "fullName", "text", new Guid("22222222-2222-2222-2222-222222222222"), "يجب أن يطابق الاسم المسجل في الهوية الوطنية", true, true, "الاسم الكامل", null, "أدخل اسمك الرباعي كما في الهوية", "{\"minLength\":3,\"maxLength\":100}" },
                    { new Guid("33333333-0001-0001-0001-000000000002"), null, 2, "nationalNumber", "text", new Guid("22222222-2222-2222-2222-222222222222"), null, true, true, "رقم الهوية الوطنية", null, "1234567890", "{\"length\":10,\"pattern\":\"^[0-9]{10}$\"}" },
                    { new Guid("33333333-0001-0001-0001-000000000003"), null, 3, "currentPassportNumber", "text", new Guid("22222222-2222-2222-2222-222222222222"), null, true, true, "رقم جواز السفر الحالي", null, "ABC123456", "{\"pattern\":\"^[A-Z]{3}[0-9]{6}$\",\"customMessage\":\"يجب أن يكون رقم الجواز بصيغة ABC123456\"}" },
                    { new Guid("33333333-0001-0001-0001-000000000004"), null, 4, "phoneNumber", "tel", new Guid("22222222-2222-2222-2222-222222222222"), null, true, true, "رقم الجوال", null, "0501234567", "{\"pattern\":\"^05[0-9]{8}$\"}" },
                    { new Guid("33333333-0001-0001-0001-000000000005"), null, 5, "email", "email", new Guid("22222222-2222-2222-2222-222222222222"), null, true, true, "البريد الإلكتروني", null, "example@email.com", "{\"pattern\":\"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\\\.[a-zA-Z]{2,}$\"}" },
                    { new Guid("33333333-0001-0001-0001-000000000006"), null, 6, "personalPhoto", "file", new Guid("22222222-2222-2222-2222-222222222222"), "صورة بخلفية بيضاء، مقاس 4×6 سم", true, true, "صورة شخصية حديثة", "{\"accept\":\".jpg,.jpeg,.png\",\"maxSizeMB\":5}", null, "{\"maxSize\":5242880,\"allowedTypes\":[\"image/jpeg\",\"image/png\"]}" },
                    { new Guid("33333333-0001-0001-0001-000000000007"), null, 7, "currentAddress", "textarea", new Guid("22222222-2222-2222-2222-222222222222"), null, true, true, "العنوان الحالي", null, "أدخل عنوانك بالتفصيل", "{\"minLength\":10,\"maxLength\":200}" },
                    { new Guid("33333333-0001-0001-0001-000000000008"), null, 8, "preferredPickupDate", "date", new Guid("22222222-2222-2222-2222-222222222222"), "سيتم إشعارك بالموعد النهائي خلال 48 ساعة", true, true, "تاريخ الاستلام المفضل", null, null, "{\"minDate\":\"today+7\",\"maxDate\":\"today+30\"}" },
                    { new Guid("33333333-0001-0001-0001-000000000009"), null, 9, "maritalStatus", "select", new Guid("22222222-2222-2222-2222-222222222222"), null, true, true, "الحالة الاجتماعية", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "ServiceFieldOptions",
                columns: new[] { "Id", "DisplayOrder", "IsActive", "OptionLabel", "OptionValue", "ServiceFormFieldId" },
                values: new object[,]
                {
                    { new Guid("44444444-0001-0001-0001-000000000001"), 1, true, "أعزب/عزباء", "single", new Guid("33333333-0001-0001-0001-000000000009") },
                    { new Guid("44444444-0001-0001-0001-000000000002"), 2, true, "متزوج/متزوجة", "married", new Guid("33333333-0001-0001-0001-000000000009") },
                    { new Guid("44444444-0001-0001-0001-000000000003"), 3, true, "مطلق/مطلقة", "divorced", new Guid("33333333-0001-0001-0001-000000000009") },
                    { new Guid("44444444-0001-0001-0001-000000000004"), 4, true, "أرمل/أرملة", "widowed", new Guid("33333333-0001-0001-0001-000000000009") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ServiceRequestId",
                table: "Appointments",
                column: "ServiceRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ServiceSlotId",
                table: "Appointments",
                column: "ServiceSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_ServiceRequestId",
                table: "Attachments",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_EntityId_Active",
                table: "Branches",
                columns: new[] { "GovernmentEntityId", "IsActive" },
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenPhones_NationalNumber",
                table: "CitizenPhones",
                column: "CitizenNationalNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CriminalRecords_NationalNumber",
                table: "CriminalRecords",
                column: "CitizenNationalNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CriminalRecords_NationalNumber_Active",
                table: "CriminalRecords",
                columns: new[] { "CitizenNationalNumber", "IsActive" },
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_FAQ_Active_Order",
                table: "FAQ",
                columns: new[] { "IsActive", "DisplayOrder" },
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_GovernmentServices_EntityId",
                table: "GovernmentServices",
                column: "GovernmentEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernmentServices_IsActive",
                table: "GovernmentServices",
                column: "IsActive",
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" },
                filter: "[IsRead] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OtpVerifications_NationalNumber_Active",
                table: "OtpVerifications",
                columns: new[] { "NationalNumber", "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestAuditLogs_RequestId_CreatedAt",
                table: "RequestAuditLogs",
                columns: new[] { "ServiceRequestId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceFieldOptions_FieldId_Active_Order",
                table: "ServiceFieldOptions",
                columns: new[] { "ServiceFormFieldId", "IsActive", "DisplayOrder" },
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceFieldOptions_FieldId_Value",
                table: "ServiceFieldOptions",
                columns: new[] { "ServiceFormFieldId", "OptionValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceFormFields_ServiceId_Active_Order",
                table: "ServiceFormFields",
                columns: new[] { "GovernmentServiceId", "IsActive", "DisplayOrder" },
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceFormFields_ServiceId_FieldName",
                table: "ServiceFormFields",
                columns: new[] { "GovernmentServiceId", "FieldName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_BranchId",
                table: "ServiceRequests",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_ReferenceNumber",
                table: "ServiceRequests",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_ServiceId_SubmissionDate",
                table: "ServiceRequests",
                columns: new[] { "GovernmentServiceId", "SubmissionDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_Status_SubmissionDate",
                table: "ServiceRequests",
                columns: new[] { "Status", "SubmissionDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_UserId_SubmissionDate",
                table: "ServiceRequests",
                columns: new[] { "UserId", "SubmissionDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSlots_ServiceId_Date",
                table: "ServiceSlots",
                columns: new[] { "GovernmentServiceId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NationalNumber",
                table: "Users",
                column: "NationalNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_ServiceRequestId",
                table: "WalletTransactions",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_WalletId_CreatedAt",
                table: "WalletTransactions",
                columns: new[] { "WalletId", "CreatedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "CitizenPhones");

            migrationBuilder.DropTable(
                name: "CriminalRecords");

            migrationBuilder.DropTable(
                name: "FAQ");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OtpVerifications");

            migrationBuilder.DropTable(
                name: "RequestAuditLogs");

            migrationBuilder.DropTable(
                name: "ServiceFieldOptions");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "ServiceSlots");

            migrationBuilder.DropTable(
                name: "ServiceFormFields");

            migrationBuilder.DropTable(
                name: "ServiceRequests");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "GovernmentServices");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "GovernmentEntities");

            migrationBuilder.DropTable(
                name: "Citizens");
        }
    }
}
