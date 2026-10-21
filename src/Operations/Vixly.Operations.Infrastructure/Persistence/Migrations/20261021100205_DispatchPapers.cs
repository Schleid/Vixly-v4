using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DispatchPapers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChargesCollect",
                schema: "ops",
                table: "Dispatches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "CostComputed",
                schema: "ops",
                table: "Dispatches",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCurrencyCode",
                schema: "ops",
                table: "Dispatches",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostNote",
                schema: "ops",
                table: "Dispatches",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostTotal",
                schema: "ops",
                table: "Dispatches",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Documents",
                schema: "ops",
                table: "Dispatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExportReference",
                schema: "ops",
                table: "Dispatches",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandlingInstructions",
                schema: "ops",
                table: "Dispatches",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DispatchShipments",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HouseNumber = table.Column<string>(type: "varchar(48)", unicode: false, maxLength: 48, nullable: false),
                    Pieces = table.Column<int>(type: "int", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    ChargeableKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    CostShare = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchShipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchShipments_Dispatches_DispatchId",
                        column: x => x.DispatchId,
                        principalSchema: "ops",
                        principalTable: "Dispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchShipments_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "ops",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchShipments_DispatchId",
                schema: "ops",
                table: "DispatchShipments",
                column: "DispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchShipments_ShipmentId",
                schema: "ops",
                table: "DispatchShipments",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchShipments_TenantId",
                schema: "ops",
                table: "DispatchShipments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchShipments_TenantId_DispatchId_ShipmentId",
                schema: "ops",
                table: "DispatchShipments",
                columns: new[] { "TenantId", "DispatchId", "ShipmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchShipments_TenantId_HouseNumber",
                schema: "ops",
                table: "DispatchShipments",
                columns: new[] { "TenantId", "HouseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchShipments_TenantId_ShipmentId",
                schema: "ops",
                table: "DispatchShipments",
                columns: new[] { "TenantId", "ShipmentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispatchShipments",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "ChargesCollect",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "CostComputed",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "CostCurrencyCode",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "CostNote",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "CostTotal",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "Documents",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "ExportReference",
                schema: "ops",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "HandlingInstructions",
                schema: "ops",
                table: "Dispatches");
        }
    }
}
