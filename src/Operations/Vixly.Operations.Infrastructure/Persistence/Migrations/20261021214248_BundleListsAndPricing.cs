using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BundleListsAndPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PublicPricing",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Published",
                schema: "ops",
                table: "RateCards",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BundleItems",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParcelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BoxId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScannedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ScannedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BundleItems_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalSchema: "ops",
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleItems_Parcels_ParcelId",
                        column: x => x.ParcelId,
                        principalSchema: "ops",
                        principalTable: "Parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleItems_RepackBoxes_BoxId",
                        column: x => x.BoxId,
                        principalSchema: "ops",
                        principalTable: "RepackBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleItems_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "ops",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_AnyTenant",
                schema: "ops",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email_AnyTenant",
                schema: "ops",
                table: "Customers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_BundleItems_BoxId",
                schema: "ops",
                table: "BundleItems",
                column: "BoxId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleItems_BundleId",
                schema: "ops",
                table: "BundleItems",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleItems_ParcelId",
                schema: "ops",
                table: "BundleItems",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleItems_ShipmentId",
                schema: "ops",
                table: "BundleItems",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleItems_TenantId",
                schema: "ops",
                table: "BundleItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleItems_TenantId_BoxId",
                schema: "ops",
                table: "BundleItems",
                columns: new[] { "TenantId", "BoxId" },
                unique: true,
                filter: "[BoxId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BundleItems_TenantId_BundleId_BoxId",
                schema: "ops",
                table: "BundleItems",
                columns: new[] { "TenantId", "BundleId", "BoxId" },
                unique: true,
                filter: "[BoxId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BundleItems_TenantId_BundleId_ParcelId",
                schema: "ops",
                table: "BundleItems",
                columns: new[] { "TenantId", "BundleId", "ParcelId" },
                unique: true,
                filter: "[ParcelId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BundleItems_TenantId_ParcelId",
                schema: "ops",
                table: "BundleItems",
                columns: new[] { "TenantId", "ParcelId" },
                unique: true,
                filter: "[ParcelId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BundleItems",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email_AnyTenant",
                schema: "ops",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Email_AnyTenant",
                schema: "ops",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PublicPricing",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "Published",
                schema: "ops",
                table: "RateCards");
        }
    }
}
