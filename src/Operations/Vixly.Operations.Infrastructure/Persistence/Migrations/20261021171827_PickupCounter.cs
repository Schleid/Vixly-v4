using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PickupCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DestinationCharges",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DestinationStorage",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DestinationStorageFeePerDay",
                schema: "ops",
                table: "TenantSettings",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DestinationStorageFreeDays",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<int>(
                name: "PickupProofs",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "CollectorName",
                schema: "ops",
                table: "Shipments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollectorPhone",
                schema: "ops",
                table: "Shipments",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DestinationBillKey",
                schema: "ops",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PickedUpUtc",
                schema: "ops",
                table: "Shipments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PickupCodeFailures",
                schema: "ops",
                table: "Shipments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PickupCodeProtected",
                schema: "ops",
                table: "Shipments",
                type: "varchar(1000)",
                unicode: false,
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PickupCodeUtc",
                schema: "ops",
                table: "Shipments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PickupReadyUtc",
                schema: "ops",
                table: "Shipments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PickupRemindersSent",
                schema: "ops",
                table: "Shipments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DestinationFeePerDay",
                schema: "ops",
                table: "Parcels",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DestinationFreeDays",
                schema: "ops",
                table: "Parcels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReadyUtc",
                schema: "ops",
                table: "Parcels",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StorageBilledUtc",
                schema: "ops",
                table: "Parcels",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PickupReleases",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Collector = table.Column<int>(type: "int", nullable: false),
                    CollectorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdKind = table.Column<int>(type: "int", nullable: true),
                    IdLast = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true),
                    SignatureKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    PhotoKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    CodeChecked = table.Column<bool>(type: "bit", nullable: false),
                    ParcelCount = table.Column<int>(type: "int", nullable: false),
                    ReleasedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickupReleases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickupReleases_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "ops",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickupReleases_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId_PickupRemindersSent_PickupReadyUtc",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "PickupRemindersSent", "PickupReadyUtc" },
                filter: "[PickupReadyUtc] IS NOT NULL AND [PickedUpUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PickupReleases_ShipmentId",
                schema: "ops",
                table: "PickupReleases",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupReleases_TenantId",
                schema: "ops",
                table: "PickupReleases",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupReleases_TenantId_RequestKey",
                schema: "ops",
                table: "PickupReleases",
                columns: new[] { "TenantId", "RequestKey" },
                unique: true,
                filter: "[RequestKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PickupReleases_TenantId_ShipmentId",
                schema: "ops",
                table: "PickupReleases",
                columns: new[] { "TenantId", "ShipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PickupReleases_WarehouseId",
                schema: "ops",
                table: "PickupReleases",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickupReleases",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_TenantId_PickupRemindersSent_PickupReadyUtc",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DestinationCharges",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "DestinationStorage",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "DestinationStorageFeePerDay",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "DestinationStorageFreeDays",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "PickupProofs",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "CollectorName",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CollectorPhone",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DestinationBillKey",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickedUpUtc",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickupCodeFailures",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickupCodeProtected",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickupCodeUtc",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickupReadyUtc",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickupRemindersSent",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DestinationFeePerDay",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "DestinationFreeDays",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "ReadyUtc",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "StorageBilledUtc",
                schema: "ops",
                table: "Parcels");
        }
    }
}
