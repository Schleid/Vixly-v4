using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Receiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParcelPrefix",
                schema: "ops",
                table: "TenantSettings",
                type: "varchar(6)",
                unicode: false,
                maxLength: 6,
                nullable: false,
                defaultValue: "PK");

            migrationBuilder.AddColumn<string>(
                name: "Carrier",
                schema: "ops",
                table: "Parcels",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarrierTrackingNumber",
                schema: "ops",
                table: "Parcels",
                type: "varchar(40)",
                unicode: false,
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                schema: "ops",
                table: "Parcels",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeclaredContents",
                schema: "ops",
                table: "Parcels",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeclaredValue",
                schema: "ops",
                table: "Parcels",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                schema: "ops",
                table: "Parcels",
                type: "decimal(7,1)",
                precision: 7,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthCm",
                schema: "ops",
                table: "Parcels",
                type: "decimal(7,1)",
                precision: 7,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Number",
                schema: "ops",
                table: "Parcels",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginKey",
                schema: "ops",
                table: "Parcels",
                type: "varchar(80)",
                unicode: false,
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreAlertId",
                schema: "ops",
                table: "Parcels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentId",
                schema: "ops",
                table: "Parcels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StorageLocationId",
                schema: "ops",
                table: "Parcels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                schema: "ops",
                table: "Parcels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                schema: "ops",
                table: "Parcels",
                type: "decimal(9,3)",
                precision: 9,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthCm",
                schema: "ops",
                table: "Parcels",
                type: "decimal(7,1)",
                precision: 7,
                scale: 1,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ParcelPhotos",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParcelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    StorageKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SizeBytes = table.Column<int>(type: "int", nullable: false),
                    Sha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    TakenByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParcelPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParcelPhotos_Parcels_ParcelId",
                        column: x => x.ParcelId,
                        principalSchema: "ops",
                        principalTable: "Parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PreAlerts",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Carrier = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TrackingNumber = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Store = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Contents = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ParcelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreAlerts_Customers_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shipments",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shipments_Customers_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StorageLocations",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Zone = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageLocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseReceipts",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParcelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Condition = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseReceipts_Parcels_ParcelId",
                        column: x => x.ParcelId,
                        principalSchema: "ops",
                        principalTable: "Parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_PreAlertId",
                schema: "ops",
                table: "Parcels",
                column: "PreAlertId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_ShipmentId",
                schema: "ops",
                table: "Parcels",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_StorageLocationId",
                schema: "ops",
                table: "Parcels",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId_Number",
                schema: "ops",
                table: "Parcels",
                columns: new[] { "TenantId", "Number" },
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId_OriginKey",
                schema: "ops",
                table: "Parcels",
                columns: new[] { "TenantId", "OriginKey" },
                unique: true,
                filter: "[OriginKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId_ShipmentId",
                schema: "ops",
                table: "Parcels",
                columns: new[] { "TenantId", "ShipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId_WarehouseId_CarrierTrackingNumber",
                schema: "ops",
                table: "Parcels",
                columns: new[] { "TenantId", "WarehouseId", "CarrierTrackingNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_WarehouseId",
                schema: "ops",
                table: "Parcels",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelPhotos_ParcelId",
                schema: "ops",
                table: "ParcelPhotos",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelPhotos_TenantId",
                schema: "ops",
                table: "ParcelPhotos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelPhotos_TenantId_ParcelId",
                schema: "ops",
                table: "ParcelPhotos",
                columns: new[] { "TenantId", "ParcelId" });

            migrationBuilder.CreateIndex(
                name: "IX_PreAlerts_TenantId",
                schema: "ops",
                table: "PreAlerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PreAlerts_TenantId_CustomerId",
                schema: "ops",
                table: "PreAlerts",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PreAlerts_TenantId_CustomerId_TrackingNumber",
                schema: "ops",
                table: "PreAlerts",
                columns: new[] { "TenantId", "CustomerId", "TrackingNumber" },
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PreAlerts_TenantId_TrackingNumber_Status",
                schema: "ops",
                table: "PreAlerts",
                columns: new[] { "TenantId", "TrackingNumber", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId",
                schema: "ops",
                table: "Shipments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId_CustomerId_OriginWarehouseId",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "CustomerId", "OriginWarehouseId" },
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_TenantId",
                schema: "ops",
                table: "StorageLocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_TenantId_WarehouseId_Code",
                schema: "ops",
                table: "StorageLocations",
                columns: new[] { "TenantId", "WarehouseId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_WarehouseId",
                schema: "ops",
                table: "StorageLocations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_ParcelId",
                schema: "ops",
                table: "WarehouseReceipts",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_TenantId",
                schema: "ops",
                table: "WarehouseReceipts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_TenantId_Number",
                schema: "ops",
                table: "WarehouseReceipts",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_TenantId_ParcelId",
                schema: "ops",
                table: "WarehouseReceipts",
                columns: new[] { "TenantId", "ParcelId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_PreAlerts_PreAlertId",
                schema: "ops",
                table: "Parcels",
                column: "PreAlertId",
                principalSchema: "ops",
                principalTable: "PreAlerts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_Shipments_ShipmentId",
                schema: "ops",
                table: "Parcels",
                column: "ShipmentId",
                principalSchema: "ops",
                principalTable: "Shipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_StorageLocations_StorageLocationId",
                schema: "ops",
                table: "Parcels",
                column: "StorageLocationId",
                principalSchema: "ops",
                principalTable: "StorageLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_Warehouses_WarehouseId",
                schema: "ops",
                table: "Parcels",
                column: "WarehouseId",
                principalSchema: "ops",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_PreAlerts_PreAlertId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_Shipments_ShipmentId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_StorageLocations_StorageLocationId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_Warehouses_WarehouseId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropTable(
                name: "ParcelPhotos",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "PreAlerts",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "Shipments",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "StorageLocations",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "WarehouseReceipts",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_PreAlertId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_ShipmentId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_StorageLocationId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_TenantId_Number",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_TenantId_OriginKey",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_TenantId_ShipmentId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_TenantId_WarehouseId_CarrierTrackingNumber",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_WarehouseId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "ParcelPrefix",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "Carrier",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "CarrierTrackingNumber",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "DeclaredContents",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "DeclaredValue",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "LengthCm",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "Number",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "OriginKey",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "PreAlertId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "ShipmentId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "StorageLocationId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                schema: "ops",
                table: "Parcels");
        }
    }
}
