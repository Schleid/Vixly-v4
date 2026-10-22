using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerScreens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerScreens",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SecretHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    PairingCodeHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    PairingExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PairedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastSeenUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Step = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SentByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SentUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ZelleAcceptedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CardChargeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerScreens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerScreens_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScreenRecords",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    ScreenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScreenName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClerkUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Language = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    TermsHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScreenRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerScreens_PairingCodeHash",
                schema: "ops",
                table: "CustomerScreens",
                column: "PairingCodeHash",
                unique: true,
                filter: "[PairingCodeHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerScreens_SecretHash",
                schema: "ops",
                table: "CustomerScreens",
                column: "SecretHash",
                unique: true,
                filter: "[SecretHash] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerScreens_TenantId",
                schema: "ops",
                table: "CustomerScreens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerScreens_TenantId_WarehouseId",
                schema: "ops",
                table: "CustomerScreens",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerScreens_WarehouseId",
                schema: "ops",
                table: "CustomerScreens",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreenRecords_TenantId",
                schema: "ops",
                table: "ScreenRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreenRecords_TenantId_InvoiceId",
                schema: "ops",
                table: "ScreenRecords",
                columns: new[] { "TenantId", "InvoiceId" },
                filter: "[InvoiceId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerScreens",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "ScreenRecords",
                schema: "ops");
        }
    }
}
