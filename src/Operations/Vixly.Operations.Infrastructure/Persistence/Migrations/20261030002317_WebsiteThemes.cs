using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WebsiteThemes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Draft_AirImageId",
                schema: "ops",
                table: "TenantBrands",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Draft_Font",
                schema: "ops",
                table: "TenantBrands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "Draft_SeaImageId",
                schema: "ops",
                table: "TenantBrands",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Draft_Theme",
                schema: "ops",
                table: "TenantBrands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "Published_AirImageId",
                schema: "ops",
                table: "TenantBrands",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Published_Font",
                schema: "ops",
                table: "TenantBrands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "Published_SeaImageId",
                schema: "ops",
                table: "TenantBrands",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Published_Theme",
                schema: "ops",
                table: "TenantBrands",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Draft_AirImageId",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_Font",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_SeaImageId",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Draft_Theme",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_AirImageId",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_Font",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_SeaImageId",
                schema: "ops",
                table: "TenantBrands");

            migrationBuilder.DropColumn(
                name: "Published_Theme",
                schema: "ops",
                table: "TenantBrands");
        }
    }
}
