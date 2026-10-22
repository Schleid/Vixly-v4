using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReceivingStations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The first 3B receipts were one per parcel; receipts are now one per carrier delivery and cannot be converted (development data only).
            migrationBuilder.Sql("DELETE FROM [ops].[WarehouseReceipts];");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseReceipts_Parcels_ParcelId",
                schema: "ops",
                table: "WarehouseReceipts");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseReceipts_ParcelId",
                schema: "ops",
                table: "WarehouseReceipts");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseReceipts_TenantId_ParcelId",
                schema: "ops",
                table: "WarehouseReceipts");

            migrationBuilder.DropColumn(
                name: "Condition",
                schema: "ops",
                table: "WarehouseReceipts");

            migrationBuilder.RenameColumn(
                name: "ParcelId",
                schema: "ops",
                table: "WarehouseReceipts",
                newName: "DeliveryId");

            migrationBuilder.AddColumn<string>(
                name: "Carrier",
                schema: "ops",
                table: "WarehouseReceipts",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DamagedCount",
                schema: "ops",
                table: "WarehouseReceipts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OpenedUtc",
                schema: "ops",
                table: "WarehouseReceipts",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "PieceCount",
                schema: "ops",
                table: "WarehouseReceipts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ArrivedDamaged",
                schema: "ops",
                table: "Parcels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryId",
                schema: "ops",
                table: "Parcels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InboundDeliveries",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Carrier = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    OpenedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClosedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpenKey = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboundDeliveries_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabelPrinters",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Connection = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Dpi = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelPrinters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabelPrinters_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrintAgents",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    KeyHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSeenUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrintAgents_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrintJobs",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrinterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Payload = table.Column<string>(type: "varchar(4000)", unicode: false, maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    TakenUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinishedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorCode = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrintJobs_LabelPrinters_PrinterId",
                        column: x => x.PrinterId,
                        principalSchema: "ops",
                        principalTable: "LabelPrinters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_DeliveryId",
                schema: "ops",
                table: "WarehouseReceipts",
                column: "DeliveryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_TenantId_WarehouseId_CreatedUtc",
                schema: "ops",
                table: "WarehouseReceipts",
                columns: new[] { "TenantId", "WarehouseId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_DeliveryId",
                schema: "ops",
                table: "Parcels",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId_DeliveryId",
                schema: "ops",
                table: "Parcels",
                columns: new[] { "TenantId", "DeliveryId" });

            migrationBuilder.CreateIndex(
                name: "IX_InboundDeliveries_TenantId",
                schema: "ops",
                table: "InboundDeliveries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundDeliveries_TenantId_OpenKey",
                schema: "ops",
                table: "InboundDeliveries",
                columns: new[] { "TenantId", "OpenKey" },
                unique: true,
                filter: "[OpenKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InboundDeliveries_TenantId_WarehouseId_Status",
                schema: "ops",
                table: "InboundDeliveries",
                columns: new[] { "TenantId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InboundDeliveries_WarehouseId",
                schema: "ops",
                table: "InboundDeliveries",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrinters_TenantId",
                schema: "ops",
                table: "LabelPrinters",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrinters_TenantId_WarehouseId",
                schema: "ops",
                table: "LabelPrinters",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrinters_WarehouseId",
                schema: "ops",
                table: "LabelPrinters",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintAgents_TenantId",
                schema: "ops",
                table: "PrintAgents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintAgents_TenantId_WarehouseId",
                schema: "ops",
                table: "PrintAgents",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrintAgents_WarehouseId",
                schema: "ops",
                table: "PrintAgents",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_PrinterId",
                schema: "ops",
                table: "PrintJobs",
                column: "PrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_TenantId",
                schema: "ops",
                table: "PrintJobs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_TenantId_WarehouseId_Status_CreatedUtc",
                schema: "ops",
                table: "PrintJobs",
                columns: new[] { "TenantId", "WarehouseId", "Status", "CreatedUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_InboundDeliveries_DeliveryId",
                schema: "ops",
                table: "Parcels",
                column: "DeliveryId",
                principalSchema: "ops",
                principalTable: "InboundDeliveries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseReceipts_InboundDeliveries_DeliveryId",
                schema: "ops",
                table: "WarehouseReceipts",
                column: "DeliveryId",
                principalSchema: "ops",
                principalTable: "InboundDeliveries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_InboundDeliveries_DeliveryId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseReceipts_InboundDeliveries_DeliveryId",
                schema: "ops",
                table: "WarehouseReceipts");

            migrationBuilder.DropTable(
                name: "InboundDeliveries",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "PrintAgents",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "PrintJobs",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "LabelPrinters",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseReceipts_DeliveryId",
                schema: "ops",
                table: "WarehouseReceipts");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseReceipts_TenantId_WarehouseId_CreatedUtc",
                schema: "ops",
                table: "WarehouseReceipts");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_DeliveryId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_TenantId_DeliveryId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "Carrier",
                schema: "ops",
                table: "WarehouseReceipts");

            migrationBuilder.DropColumn(
                name: "DamagedCount",
                schema: "ops",
                table: "WarehouseReceipts");

            migrationBuilder.DropColumn(
                name: "OpenedUtc",
                schema: "ops",
                table: "WarehouseReceipts");

            migrationBuilder.DropColumn(
                name: "PieceCount",
                schema: "ops",
                table: "WarehouseReceipts");

            migrationBuilder.DropColumn(
                name: "ArrivedDamaged",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "DeliveryId",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.RenameColumn(
                name: "DeliveryId",
                schema: "ops",
                table: "WarehouseReceipts",
                newName: "ParcelId");

            migrationBuilder.AddColumn<string>(
                name: "Condition",
                schema: "ops",
                table: "WarehouseReceipts",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_ParcelId",
                schema: "ops",
                table: "WarehouseReceipts",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_TenantId_ParcelId",
                schema: "ops",
                table: "WarehouseReceipts",
                columns: new[] { "TenantId", "ParcelId" });

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseReceipts_Parcels_ParcelId",
                schema: "ops",
                table: "WarehouseReceipts",
                column: "ParcelId",
                principalSchema: "ops",
                principalTable: "Parcels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
