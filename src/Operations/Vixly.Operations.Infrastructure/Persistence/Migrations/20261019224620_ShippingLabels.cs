using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShippingLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Number",
                schema: "ops",
                table: "Shipments",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ShippingLabelUtc",
                schema: "ops",
                table: "Parcels",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Media",
                schema: "ops",
                table: "LabelPrinters",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId_Number",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "Number" },
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId_WarehouseId_ShippingLabelUtc",
                schema: "ops",
                table: "Parcels",
                columns: new[] { "TenantId", "WarehouseId", "ShippingLabelUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_TenantId_Number",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_TenantId_WarehouseId_ShippingLabelUtc",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "Number",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ShippingLabelUtc",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "Media",
                schema: "ops",
                table: "LabelPrinters");
        }
    }
}
