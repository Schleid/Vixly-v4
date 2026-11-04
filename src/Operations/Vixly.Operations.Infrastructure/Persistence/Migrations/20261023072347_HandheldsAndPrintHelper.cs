using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HandheldsAndPrintHelper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPhone",
                schema: "ops",
                table: "PrintAgents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "AgentId",
                schema: "ops",
                table: "LabelPrinters",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PrintPairings",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintPairings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrintPairings_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShelfCounts",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scanned = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    FinishedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Found = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    Missing = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    Extra = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    Unknown = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShelfCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShelfCounts_StorageLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "ops",
                        principalTable: "StorageLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrintPairings_CodeHash",
                schema: "ops",
                table: "PrintPairings",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrintPairings_TenantId",
                schema: "ops",
                table: "PrintPairings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintPairings_WarehouseId",
                schema: "ops",
                table: "PrintPairings",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfCounts_LocationId",
                schema: "ops",
                table: "ShelfCounts",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfCounts_TenantId",
                schema: "ops",
                table: "ShelfCounts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfCounts_TenantId_LocationId",
                schema: "ops",
                table: "ShelfCounts",
                columns: new[] { "TenantId", "LocationId" },
                unique: true,
                filter: "[FinishedUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfCounts_TenantId_WarehouseId_CreatedUtc",
                schema: "ops",
                table: "ShelfCounts",
                columns: new[] { "TenantId", "WarehouseId", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrintPairings",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "ShelfCounts",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "IsPhone",
                schema: "ops",
                table: "PrintAgents");

            migrationBuilder.DropColumn(
                name: "AgentId",
                schema: "ops",
                table: "LabelPrinters");
        }
    }
}
