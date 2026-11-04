using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ParcelItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ItemCatalogueLoaded",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ItemsRequiredSinceUtc",
                schema: "ops",
                table: "TenantSettings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItemCatalogue",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Matches = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    HsCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    UseCount = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCatalogue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParcelItems",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParcelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreAlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Line = table.Column<int>(type: "int", nullable: false),
                    Words = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    ValueEach = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Used = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HsCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    OriginCountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: true),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    CatalogueEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClassifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClassifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParcelItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemCatalogue_TenantId",
                schema: "ops",
                table: "ItemCatalogue",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemCatalogue_TenantId_Description",
                schema: "ops",
                table: "ItemCatalogue",
                columns: new[] { "TenantId", "Description" });

            migrationBuilder.CreateIndex(
                name: "IX_ParcelItems_TenantId",
                schema: "ops",
                table: "ParcelItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelItems_TenantId_ParcelId",
                schema: "ops",
                table: "ParcelItems",
                columns: new[] { "TenantId", "ParcelId" });

            migrationBuilder.CreateIndex(
                name: "IX_ParcelItems_TenantId_PreAlertId",
                schema: "ops",
                table: "ParcelItems",
                columns: new[] { "TenantId", "PreAlertId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemCatalogue",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "ParcelItems",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "ItemCatalogueLoaded",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ItemsRequiredSinceUtc",
                schema: "ops",
                table: "TenantSettings");
        }
    }
}
