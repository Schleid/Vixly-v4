using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OriginIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestKey",
                schema: "ops",
                table: "Shipments",
                type: "varchar(60)",
                unicode: false,
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceCode",
                schema: "ops",
                table: "Shipments",
                type: "varchar(40)",
                unicode: false,
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                schema: "ops",
                table: "PreAlerts",
                type: "uniqueidentifier",
                nullable: true);

            // Each pre-alert gets its warehouse: its shipment's origin, else where its package arrived, else the company's first receiving warehouse.
            migrationBuilder.Sql(@"EXEC(N'UPDATE p SET [WarehouseId] = COALESCE(
    (SELECT s.[OriginWarehouseId] FROM [ops].[Shipments] s WHERE s.[Id] = p.[ShipmentId]),
    (SELECT r.[WarehouseId] FROM [ops].[Parcels] r WHERE r.[Id] = p.[ParcelId]),
    (SELECT TOP 1 w.[Id] FROM [ops].[Warehouses] w WHERE w.[TenantId] = p.[TenantId] AND w.[Status] = 0 AND (w.[Roles] & 1) = 1 ORDER BY w.[Code]))
FROM [ops].[PreAlerts] p WHERE p.[WarehouseId] IS NULL;');");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Warehouses_TenantId_Id",
                schema: "ops",
                table: "Warehouses",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId_CustomerId_RequestKey",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "CustomerId", "RequestKey" },
                unique: true,
                filter: "[RequestKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PreAlerts_TenantId_WarehouseId_Status_ExpectedDate",
                schema: "ops",
                table: "PreAlerts",
                columns: new[] { "TenantId", "WarehouseId", "Status", "ExpectedDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_PreAlerts_Warehouses_TenantId_WarehouseId",
                schema: "ops",
                table: "PreAlerts",
                columns: new[] { "TenantId", "WarehouseId" },
                principalSchema: "ops",
                principalTable: "Warehouses",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreAlerts_Warehouses_TenantId_WarehouseId",
                schema: "ops",
                table: "PreAlerts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Warehouses_TenantId_Id",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_TenantId_CustomerId_RequestKey",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_PreAlerts_TenantId_WarehouseId_Status_ExpectedDate",
                schema: "ops",
                table: "PreAlerts");

            migrationBuilder.DropColumn(
                name: "RequestKey",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ServiceCode",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                schema: "ops",
                table: "PreAlerts");
        }
    }
}
