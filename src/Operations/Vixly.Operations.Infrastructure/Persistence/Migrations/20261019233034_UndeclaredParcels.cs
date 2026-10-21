using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UndeclaredParcels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StorageFeePerDay",
                schema: "ops",
                table: "TenantSettings",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StorageFreeDays",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<string>(
                name: "LabelCode",
                schema: "ops",
                table: "Parcels",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LabelName",
                schema: "ops",
                table: "Parcels",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LabelSender",
                schema: "ops",
                table: "Parcels",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StorageFeePerDay",
                schema: "ops",
                table: "Parcels",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StorageFreeDays",
                schema: "ops",
                table: "Parcels",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockCounts",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpenWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lines = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCounts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_TenantId",
                schema: "ops",
                table: "StockCounts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_TenantId_OpenWarehouseId",
                schema: "ops",
                table: "StockCounts",
                columns: new[] { "TenantId", "OpenWarehouseId" },
                unique: true,
                filter: "[OpenWarehouseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_TenantId_WarehouseId_CreatedUtc",
                schema: "ops",
                table: "StockCounts",
                columns: new[] { "TenantId", "WarehouseId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_WarehouseId",
                schema: "ops",
                table: "StockCounts",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockCounts",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "StorageFeePerDay",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "StorageFreeDays",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "LabelCode",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "LabelName",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "LabelSender",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "StorageFeePerDay",
                schema: "ops",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "StorageFreeDays",
                schema: "ops",
                table: "Parcels");
        }
    }
}
