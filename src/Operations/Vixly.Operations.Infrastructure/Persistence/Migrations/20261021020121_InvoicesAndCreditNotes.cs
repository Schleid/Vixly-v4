using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InvoicesAndCreditNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ChargesTaxPercent",
                schema: "ops",
                table: "TenantSettings",
                type: "decimal(6,3)",
                precision: 6,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CreditNotePrefix",
                schema: "ops",
                table: "TenantSettings",
                type: "varchar(6)",
                unicode: false,
                maxLength: 6,
                nullable: false,
                defaultValue: "CN");

            // A company already using CN for another kind of number gets CRN (CRNT when CRN is taken too), so its prefixes stay different. In EXEC: compiled after the column exists.
            migrationBuilder.Sql(@"EXEC(N'UPDATE [ops].[TenantSettings] SET [CreditNotePrefix] = CASE WHEN N''CRN'' IN ([ShipmentPrefix], [ReceivingPrefix], [InvoicePrefix], [PaymentPrefix], [CustomerPrefix], [ParcelPrefix], [SupplyOrderPrefix]) THEN N''CRNT'' ELSE N''CRN'' END WHERE N''CN'' IN ([ShipmentPrefix], [ReceivingPrefix], [InvoicePrefix], [PaymentPrefix], [CustomerPrefix], [ParcelPrefix], [SupplyOrderPrefix])');");

            migrationBuilder.AddColumn<decimal>(
                name: "FreightTaxPercent",
                schema: "ops",
                table: "TenantSettings",
                type: "decimal(6,3)",
                precision: 6,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceTaxPercent",
                schema: "ops",
                table: "TenantSettings",
                type: "decimal(6,3)",
                precision: 6,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNoteEn",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNoteEs",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNoteFr",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SuppliesTaxPercent",
                schema: "ops",
                table: "TenantSettings",
                type: "decimal(6,3)",
                precision: 6,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TaxName",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TaxPerKind",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplySaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reference = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ServiceCode = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Language = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    WarehouseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerCompany = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomerPhone = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    CustomerEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    TaxName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    TaxPerKind = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IssuedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    IssuedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VoidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Gross = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    CreditedTotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    PaidTotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lines = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Taxes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "ops",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_SupplySales_SupplySaleId",
                        column: x => x.SupplySaleId,
                        principalSchema: "ops",
                        principalTable: "SupplySales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RefusalNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Total = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    DecidedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lines = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Taxes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "ops",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_InvoiceId",
                schema: "ops",
                table: "CreditNotes",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId",
                schema: "ops",
                table: "CreditNotes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId_InvoiceId",
                schema: "ops",
                table: "CreditNotes",
                columns: new[] { "TenantId", "InvoiceId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId_InvoiceId_Status",
                schema: "ops",
                table: "CreditNotes",
                columns: new[] { "TenantId", "InvoiceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId_Number",
                schema: "ops",
                table: "CreditNotes",
                columns: new[] { "TenantId", "Number" },
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId_RequestKey",
                schema: "ops",
                table: "CreditNotes",
                columns: new[] { "TenantId", "RequestKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId_WarehouseId_Status",
                schema: "ops",
                table: "CreditNotes",
                columns: new[] { "TenantId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                schema: "ops",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ShipmentId",
                schema: "ops",
                table: "Invoices",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SupplySaleId",
                schema: "ops",
                table: "Invoices",
                column: "SupplySaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId",
                schema: "ops",
                table: "Invoices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_CustomerId_IssuedUtc",
                schema: "ops",
                table: "Invoices",
                columns: new[] { "TenantId", "CustomerId", "IssuedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_Number",
                schema: "ops",
                table: "Invoices",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_ShipmentId",
                schema: "ops",
                table: "Invoices",
                columns: new[] { "TenantId", "ShipmentId" },
                unique: true,
                filter: "[ShipmentId] IS NOT NULL AND [Kind] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_SupplySaleId",
                schema: "ops",
                table: "Invoices",
                columns: new[] { "TenantId", "SupplySaleId" },
                unique: true,
                filter: "[SupplySaleId] IS NOT NULL AND [Kind] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_WarehouseId_IssuedUtc",
                schema: "ops",
                table: "Invoices",
                columns: new[] { "TenantId", "WarehouseId", "IssuedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_WarehouseId",
                schema: "ops",
                table: "Invoices",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditNotes",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "ChargesTaxPercent",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "CreditNotePrefix",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "FreightTaxPercent",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "InsuranceTaxPercent",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceNoteEn",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceNoteEs",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceNoteFr",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "SuppliesTaxPercent",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "TaxName",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "TaxPerKind",
                schema: "ops",
                table: "TenantSettings");
        }
    }
}
