using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tickets",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Carrier = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TrackingNumber = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Store = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrderNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    DeliveredOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Contents = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ProofKey = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ProofContentType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ProofSha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    ParcelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastCommandKey = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Messages = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CustomerId",
                schema: "ops",
                table: "Tickets",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TenantId",
                schema: "ops",
                table: "Tickets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TenantId_CustomerId_CreatedUtc",
                schema: "ops",
                table: "Tickets",
                columns: new[] { "TenantId", "CustomerId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TenantId_CustomerId_TrackingNumber",
                schema: "ops",
                table: "Tickets",
                columns: new[] { "TenantId", "CustomerId", "TrackingNumber" },
                unique: true,
                filter: "[Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TenantId_WarehouseId_Status_CreatedUtc",
                schema: "ops",
                table: "Tickets",
                columns: new[] { "TenantId", "WarehouseId", "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_WarehouseId",
                schema: "ops",
                table: "Tickets",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tickets",
                schema: "ops");
        }
    }
}
