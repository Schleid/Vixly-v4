using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Billing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TaxExempt",
                schema: "platform",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TaxExemptionReason",
                schema: "platform",
                table: "Tenants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxExemptionReference",
                schema: "platform",
                table: "Tenants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                schema: "platform",
                table: "Tenants",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingSettings",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    PaymentTermsDays = table.Column<int>(type: "int", nullable: true),
                    InvoicePrefix = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    CreditNotePrefix = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    ReceiptPrefix = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    PaperSize = table.Column<int>(type: "int", nullable: false),
                    EmailDocuments = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingSettings_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyProfile",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Logo = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LogoContentType = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    TradingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StateProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyProfile_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    SourceKey = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Language = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    IssuedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DueUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PeriodStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PeriodEndUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NetTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditedTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PastDueUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CustomerAddress = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    CustomerEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerTaxId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Subscriptions_PlatformSubscriptionId",
                        column: x => x.PlatformSubscriptionId,
                        principalSchema: "platform",
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NumberSequences",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    NextValue = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxRates",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    RegionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ValidFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxRates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingTexts",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Language = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    PaymentInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Footer = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BillingSettingsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingTexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingTexts_BillingSettings_BillingSettingsId",
                        column: x => x.BillingSettingsId,
                        principalSchema: "platform",
                        principalTable: "BillingSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingDocuments",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    PlatformInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Language = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingDocuments_Invoices_PlatformInvoiceId",
                        column: x => x.PlatformInvoiceId,
                        principalSchema: "platform",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxPart = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsVoid = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Invoices_PlatformInvoiceId",
                        column: x => x.PlatformInvoiceId,
                        principalSchema: "platform",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceEvents",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Arguments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceEvents_Invoices_PlatformInvoiceId",
                        column: x => x.PlatformInvoiceId,
                        principalSchema: "platform",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    TextKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Arguments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Net = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxLabel = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_PlatformInvoiceId",
                        column: x => x.PlatformInvoiceId,
                        principalSchema: "platform",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    ReceivedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReversedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReversalReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices_PlatformInvoiceId",
                        column: x => x.PlatformInvoiceId,
                        principalSchema: "platform",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payments_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Users_ReversedByUserId",
                        column: x => x.ReversedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingDocuments_Kind_OwnerId",
                schema: "platform",
                table: "BillingDocuments",
                columns: new[] { "Kind", "OwnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingDocuments_PlatformInvoiceId",
                schema: "platform",
                table: "BillingDocuments",
                column: "PlatformInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSettings_Key",
                schema: "platform",
                table: "BillingSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingSettings_UpdatedByUserId",
                schema: "platform",
                table: "BillingSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingTexts_BillingSettingsId",
                schema: "platform",
                table: "BillingTexts",
                column: "BillingSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfile_Key",
                schema: "platform",
                table: "CompanyProfile",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfile_UpdatedByUserId",
                schema: "platform",
                table: "CompanyProfile",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_DecidedByUserId",
                schema: "platform",
                table: "CreditNotes",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_Number",
                schema: "platform",
                table: "CreditNotes",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_OnePending",
                schema: "platform",
                table: "CreditNotes",
                column: "PlatformInvoiceId",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_RequestedByUserId",
                schema: "platform",
                table: "CreditNotes",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceEvents_PlatformInvoiceId_CreatedUtc",
                schema: "platform",
                table: "InvoiceEvents",
                columns: new[] { "PlatformInvoiceId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_PlatformInvoiceId",
                schema: "platform",
                table: "InvoiceLines",
                column: "PlatformInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Number",
                schema: "platform",
                table: "Invoices",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PlatformSubscriptionId_Status",
                schema: "platform",
                table: "Invoices",
                columns: new[] { "PlatformSubscriptionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PlatformTenantId_IssuedUtc",
                schema: "platform",
                table: "Invoices",
                columns: new[] { "PlatformTenantId", "IssuedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SourceKey",
                schema: "platform",
                table: "Invoices",
                column: "SourceKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status_DueUtc",
                schema: "platform",
                table: "Invoices",
                columns: new[] { "Status", "DueUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_Kind_Year",
                schema: "platform",
                table: "NumberSequences",
                columns: new[] { "Kind", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PlatformInvoiceId",
                schema: "platform",
                table: "Payments",
                column: "PlatformInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceiptNumber",
                schema: "platform",
                table: "Payments",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceivedUtc",
                schema: "platform",
                table: "Payments",
                column: "ReceivedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RecordedByUserId",
                schema: "platform",
                table: "Payments",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReversedByUserId",
                schema: "platform",
                table: "Payments",
                column: "ReversedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_CountryCode_RegionCode_ValidFromUtc",
                schema: "platform",
                table: "TaxRates",
                columns: new[] { "CountryCode", "RegionCode", "ValidFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_CreatedByUserId",
                schema: "platform",
                table: "TaxRates",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingDocuments",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "BillingTexts",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "CompanyProfile",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "CreditNotes",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "InvoiceEvents",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "InvoiceLines",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "NumberSequences",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TaxRates",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "BillingSettings",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "TaxExempt",
                schema: "platform",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TaxExemptionReason",
                schema: "platform",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TaxExemptionReference",
                schema: "platform",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TaxId",
                schema: "platform",
                table: "Tenants");
        }
    }
}
