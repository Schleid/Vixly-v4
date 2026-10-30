using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CashierAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptBankTransfer",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptCash",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptCheque",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptZelle",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OtherMethod1",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherMethod2",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherMethod3",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZelleContact",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZelleName",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZelleQrKey",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZelleTag",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZelleTermsEn",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZelleTermsEs",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZelleTermsFr",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PendingTotal",
                schema: "ops",
                table: "Invoices",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedTotal",
                schema: "ops",
                table: "Invoices",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CashDrawers",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Float = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    CashIn = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    CashOut = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Counted = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    Difference = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    CloseNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OpenedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashDrawers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashDrawers_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DrawerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TakenByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TakenUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Pending = table.Column<bool>(type: "bit", nullable: false),
                    ZelleTermsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tenders = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_CashDrawers_DrawerId",
                        column: x => x.DrawerId,
                        principalSchema: "ops",
                        principalTable: "CashDrawers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "ops",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    DrawerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_CashDrawers_DrawerId",
                        column: x => x.DrawerId,
                        principalSchema: "ops",
                        principalTable: "CashDrawers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refunds_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "ops",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawers_TenantId",
                schema: "ops",
                table: "CashDrawers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawers_TenantId_UserId_WarehouseId",
                schema: "ops",
                table: "CashDrawers",
                columns: new[] { "TenantId", "UserId", "WarehouseId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawers_TenantId_WarehouseId_OpenedUtc",
                schema: "ops",
                table: "CashDrawers",
                columns: new[] { "TenantId", "WarehouseId", "OpenedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawers_WarehouseId",
                schema: "ops",
                table: "CashDrawers",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId",
                schema: "ops",
                table: "Payments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DrawerId",
                schema: "ops",
                table: "Payments",
                column: "DrawerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceId",
                schema: "ops",
                table: "Payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId",
                schema: "ops",
                table: "Payments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_InvoiceId",
                schema: "ops",
                table: "Payments",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_Number",
                schema: "ops",
                table: "Payments",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_RequestKey",
                schema: "ops",
                table: "Payments",
                columns: new[] { "TenantId", "RequestKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_WarehouseId_Pending",
                schema: "ops",
                table: "Payments",
                columns: new[] { "TenantId", "WarehouseId", "Pending" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_WarehouseId",
                schema: "ops",
                table: "Payments",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_DrawerId",
                schema: "ops",
                table: "Refunds",
                column: "DrawerId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_InvoiceId",
                schema: "ops",
                table: "Refunds",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_TenantId",
                schema: "ops",
                table: "Refunds",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_TenantId_InvoiceId",
                schema: "ops",
                table: "Refunds",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_TenantId_Number",
                schema: "ops",
                table: "Refunds",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_TenantId_RequestKey",
                schema: "ops",
                table: "Refunds",
                columns: new[] { "TenantId", "RequestKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "Refunds",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "CashDrawers",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "AcceptBankTransfer",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "AcceptCash",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "AcceptCheque",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "AcceptZelle",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "OtherMethod1",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "OtherMethod2",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "OtherMethod3",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ZelleContact",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ZelleName",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ZelleQrKey",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ZelleTag",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ZelleTermsEn",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ZelleTermsEs",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ZelleTermsFr",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "PendingTotal",
                schema: "ops",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RefundedTotal",
                schema: "ops",
                table: "Invoices");
        }
    }
}
