using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerShipments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_TenantId_CustomerId_OriginWarehouseId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress_City",
                schema: "ops",
                table: "Shipments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress_CountryCode",
                schema: "ops",
                table: "Shipments",
                type: "char(2)",
                unicode: false,
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress_Line1",
                schema: "ops",
                table: "Shipments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress_Line2",
                schema: "ops",
                table: "Shipments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress_PostalCode",
                schema: "ops",
                table: "Shipments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress_Region",
                schema: "ops",
                table: "Shipments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DestinationBranchId",
                schema: "ops",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationCountryCode",
                schema: "ops",
                table: "Shipments",
                type: "char(2)",
                unicode: false,
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Options",
                schema: "ops",
                table: "Shipments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                schema: "ops",
                table: "Shipments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientPhone",
                schema: "ops",
                table: "Shipments",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentId",
                schema: "ops",
                table: "PreAlerts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Shipments_TenantId_Id",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Branches_TenantId_Id",
                schema: "ops",
                table: "Branches",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId_CustomerId_OriginWarehouseId",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "CustomerId", "OriginWarehouseId" },
                unique: true,
                filter: "[Status] = 0 AND [DestinationCountryCode] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId_CustomerId_Status",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId_DestinationBranchId",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "DestinationBranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_PreAlerts_TenantId_ShipmentId",
                schema: "ops",
                table: "PreAlerts",
                columns: new[] { "TenantId", "ShipmentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PreAlerts_Shipments_TenantId_ShipmentId",
                schema: "ops",
                table: "PreAlerts",
                columns: new[] { "TenantId", "ShipmentId" },
                principalSchema: "ops",
                principalTable: "Shipments",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Branches_TenantId_DestinationBranchId",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "DestinationBranchId" },
                principalSchema: "ops",
                principalTable: "Branches",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreAlerts_Shipments_TenantId_ShipmentId",
                schema: "ops",
                table: "PreAlerts");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Branches_TenantId_DestinationBranchId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Shipments_TenantId_Id",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_TenantId_CustomerId_OriginWarehouseId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_TenantId_CustomerId_Status",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_TenantId_DestinationBranchId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_PreAlerts_TenantId_ShipmentId",
                schema: "ops",
                table: "PreAlerts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Branches_TenantId_Id",
                schema: "ops",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_City",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_CountryCode",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_Line1",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_Line2",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_PostalCode",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_Region",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DestinationBranchId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DestinationCountryCode",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Options",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RecipientName",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RecipientPhone",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ShipmentId",
                schema: "ops",
                table: "PreAlerts");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId_CustomerId_OriginWarehouseId",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "CustomerId", "OriginWarehouseId" },
                unique: true,
                filter: "[Status] = 0");
        }
    }
}
