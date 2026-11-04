using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackingAndDocumentLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DocumentLogoByUserId",
                schema: "ops",
                table: "TenantSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentLogoImageId",
                schema: "ops",
                table: "TenantSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DocumentLogoUtc",
                schema: "ops",
                table: "TenantSettings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LabelLogoImageId",
                schema: "ops",
                table: "TenantSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingPrefix",
                schema: "ops",
                table: "TenantSettings",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: false,
                defaultValue: "VX");

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                schema: "ops",
                table: "Shipments",
                type: "varchar(13)",
                unicode: false,
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TrackingNumberUtc",
                schema: "ops",
                table: "Shipments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TrackingNumber",
                schema: "ops",
                table: "Shipments",
                column: "TrackingNumber",
                unique: true,
                filter: "[TrackingNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_TrackingNumber",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DocumentLogoByUserId",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "DocumentLogoImageId",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "DocumentLogoUtc",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "LabelLogoImageId",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "TrackingPrefix",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "TrackingNumber",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "TrackingNumberUtc",
                schema: "ops",
                table: "Shipments");
        }
    }
}
