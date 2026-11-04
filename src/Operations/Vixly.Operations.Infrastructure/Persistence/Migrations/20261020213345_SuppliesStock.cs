using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SuppliesStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CountSupplyStock",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplyStockOut",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "SupplyCounts",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpenedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Cancelled = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lines = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyCounts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplyMovements",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: true),
                    Reason = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Supplier = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    LinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyMovements_Supplies_SupplyId",
                        column: x => x.SupplyId,
                        principalSchema: "ops",
                        principalTable: "Supplies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyMovements_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplyStocks",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnHand = table.Column<int>(type: "int", nullable: false),
                    ReorderLevel = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyStocks_Supplies_SupplyId",
                        column: x => x.SupplyId,
                        principalSchema: "ops",
                        principalTable: "Supplies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyStocks_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplyTransfers",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SentByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lines = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyTransfers_Warehouses_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyTransfers_Warehouses_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyCounts_TenantId",
                schema: "ops",
                table: "SupplyCounts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyCounts_TenantId_WarehouseId_ClosedUtc",
                schema: "ops",
                table: "SupplyCounts",
                columns: new[] { "TenantId", "WarehouseId", "ClosedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyCounts_WarehouseId",
                schema: "ops",
                table: "SupplyCounts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyMovements_SupplyId",
                schema: "ops",
                table: "SupplyMovements",
                column: "SupplyId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyMovements_TenantId",
                schema: "ops",
                table: "SupplyMovements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyMovements_TenantId_LinkId",
                schema: "ops",
                table: "SupplyMovements",
                columns: new[] { "TenantId", "LinkId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyMovements_TenantId_SupplyId_WarehouseId_CreatedUtc",
                schema: "ops",
                table: "SupplyMovements",
                columns: new[] { "TenantId", "SupplyId", "WarehouseId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyMovements_WarehouseId",
                schema: "ops",
                table: "SupplyMovements",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyStocks_SupplyId",
                schema: "ops",
                table: "SupplyStocks",
                column: "SupplyId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyStocks_TenantId",
                schema: "ops",
                table: "SupplyStocks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyStocks_TenantId_SupplyId_WarehouseId",
                schema: "ops",
                table: "SupplyStocks",
                columns: new[] { "TenantId", "SupplyId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyStocks_TenantId_WarehouseId",
                schema: "ops",
                table: "SupplyStocks",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyStocks_WarehouseId",
                schema: "ops",
                table: "SupplyStocks",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyTransfers_FromWarehouseId",
                schema: "ops",
                table: "SupplyTransfers",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyTransfers_TenantId",
                schema: "ops",
                table: "SupplyTransfers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyTransfers_TenantId_ReceivedUtc",
                schema: "ops",
                table: "SupplyTransfers",
                columns: new[] { "TenantId", "ReceivedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyTransfers_ToWarehouseId",
                schema: "ops",
                table: "SupplyTransfers",
                column: "ToWarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplyCounts",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "SupplyMovements",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "SupplyStocks",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "SupplyTransfers",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "CountSupplyStock",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "SupplyStockOut",
                schema: "ops",
                table: "TenantSettings");
        }
    }
}
