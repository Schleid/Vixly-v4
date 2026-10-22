using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HomeDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AfterFailedDelivery",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "CashOnDelivery",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DeliveryEmails",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryProofs",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryRecording",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryTries",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredUtc",
                schema: "ops",
                table: "Shipments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryTries",
                schema: "ops",
                table: "Parcels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RouteId",
                schema: "ops",
                table: "Parcels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryRoutes",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DriverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StopCount = table.Column<int>(type: "int", nullable: false),
                    CashCollected = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    CashHandedIn = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    SettleNote = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReturnedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SettledUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SettledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryRoutes_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryStops",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ParcelCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReceiverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdKind = table.Column<int>(type: "int", nullable: true),
                    IdLast = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true),
                    SignatureKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    PhotoKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    CodeChecked = table.Column<bool>(type: "bit", nullable: false),
                    FromSheet = table.Column<bool>(type: "bit", nullable: false),
                    CashCollected = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    CollectedInvoices = table.Column<string>(type: "varchar(400)", unicode: false, maxLength: 400, nullable: true),
                    FailureReason = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DoneUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DoneByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryStops_DeliveryRoutes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "ops",
                        principalTable: "DeliveryRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryStops_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "ops",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_RouteId",
                schema: "ops",
                table: "Parcels",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId_RouteId",
                schema: "ops",
                table: "Parcels",
                columns: new[] { "TenantId", "RouteId" },
                filter: "[RouteId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRoutes_TenantId",
                schema: "ops",
                table: "DeliveryRoutes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRoutes_TenantId_DriverUserId_ServiceDate",
                schema: "ops",
                table: "DeliveryRoutes",
                columns: new[] { "TenantId", "DriverUserId", "ServiceDate" },
                filter: "[DriverUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRoutes_TenantId_Number",
                schema: "ops",
                table: "DeliveryRoutes",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRoutes_TenantId_RequestKey",
                schema: "ops",
                table: "DeliveryRoutes",
                columns: new[] { "TenantId", "RequestKey" },
                unique: true,
                filter: "[RequestKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRoutes_TenantId_WarehouseId_ServiceDate",
                schema: "ops",
                table: "DeliveryRoutes",
                columns: new[] { "TenantId", "WarehouseId", "ServiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRoutes_WarehouseId",
                schema: "ops",
                table: "DeliveryRoutes",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryStops_RouteId",
                schema: "ops",
                table: "DeliveryStops",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryStops_ShipmentId",
                schema: "ops",
                table: "DeliveryStops",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryStops_TenantId",
                schema: "ops",
                table: "DeliveryStops",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryStops_TenantId_RouteId_ShipmentId",
                schema: "ops",
                table: "DeliveryStops",
                columns: new[] { "TenantId", "RouteId", "ShipmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryStops_TenantId_ShipmentId",
                schema: "ops",
                table: "DeliveryStops",
                columns: new[] { "TenantId", "ShipmentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_DeliveryRoutes_RouteId",
                schema: "ops",
                table: "Parcels",
                column: "RouteId",
                principalSchema: "ops",
                principalTable: "DeliveryRoutes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_DeliveryRoutes_RouteId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropTable(
                name: "DeliveryStops",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "DeliveryRoutes",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_RouteId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_TenantId_RouteId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "AfterFailedDelivery",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "CashOnDelivery",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryEmails",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryProofs",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryRecording",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryTries",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "DeliveredUtc",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryTries",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "RouteId",
                schema: "ops",
                table: "Parcels");
        }
    }
}
