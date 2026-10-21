using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SuppliesAtCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SupplyDiscountLinePercent",
                schema: "ops",
                table: "TenantSettings",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<decimal>(
                name: "SupplyDiscountOrderPercent",
                schema: "ops",
                table: "TenantSettings",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<bool>(
                name: "SupplyDiscountReasonRequired",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "SupplyDiscountsAllowed",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplyOrderPrefix",
                schema: "ops",
                table: "TenantSettings",
                type: "varchar(6)",
                unicode: false,
                maxLength: 6,
                nullable: false,
                defaultValue: "SO");

            // A company already using SO for another kind of number gets SPO, so its prefixes stay different (review of 5E). In EXEC: compiled after the column exists.
            migrationBuilder.Sql(@"EXEC(N'UPDATE [ops].[TenantSettings] SET [SupplyOrderPrefix] = N''SPO'' WHERE N''SO'' IN ([ShipmentPrefix], [ReceivingPrefix], [InvoicePrefix], [PaymentPrefix], [CustomerPrefix], [ParcelPrefix])');");

            migrationBuilder.CreateTable(
                name: "SupplySales",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StockMoved = table.Column<bool>(type: "bit", nullable: false),
                    LastKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lines = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplySales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplySales_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplySales_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "ops",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplySales_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplySales_CustomerId",
                schema: "ops",
                table: "SupplySales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplySales_ShipmentId",
                schema: "ops",
                table: "SupplySales",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplySales_TenantId",
                schema: "ops",
                table: "SupplySales",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplySales_TenantId_CustomerId_Status",
                schema: "ops",
                table: "SupplySales",
                columns: new[] { "TenantId", "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplySales_TenantId_Number",
                schema: "ops",
                table: "SupplySales",
                columns: new[] { "TenantId", "Number" },
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplySales_TenantId_ShipmentId",
                schema: "ops",
                table: "SupplySales",
                columns: new[] { "TenantId", "ShipmentId" },
                unique: true,
                filter: "[ShipmentId] IS NOT NULL AND [Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_SupplySales_WarehouseId",
                schema: "ops",
                table: "SupplySales",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplySales",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "SupplyDiscountLinePercent",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "SupplyDiscountOrderPercent",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "SupplyDiscountReasonRequired",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "SupplyDiscountsAllowed",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "SupplyOrderPrefix",
                schema: "ops",
                table: "TenantSettings");
        }
    }
}
