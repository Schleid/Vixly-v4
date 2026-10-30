using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FrontDeskCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookedCurrencyCode",
                schema: "ops",
                table: "Shipments",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BookedTotal",
                schema: "ops",
                table: "Shipments",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BookedUtc",
                schema: "ops",
                table: "Shipments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CreatedAtCounter",
                schema: "ops",
                table: "Shipments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParcelCount",
                schema: "ops",
                table: "Shipments",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookedCurrencyCode",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "BookedTotal",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "BookedUtc",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CreatedAtCounter",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ParcelCount",
                schema: "ops",
                table: "Shipments");
        }
    }
}
