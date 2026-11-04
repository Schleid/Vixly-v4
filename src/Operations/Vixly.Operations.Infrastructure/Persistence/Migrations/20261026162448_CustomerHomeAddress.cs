using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerHomeAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HomeAddressUtc",
                schema: "ops",
                table: "Customers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Home_City",
                schema: "ops",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Home_CountryCode",
                schema: "ops",
                table: "Customers",
                type: "char(2)",
                unicode: false,
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Home_Line1",
                schema: "ops",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Home_Line2",
                schema: "ops",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Home_PostalCode",
                schema: "ops",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Home_Region",
                schema: "ops",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HomeAddressUtc",
                schema: "ops",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Home_City",
                schema: "ops",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Home_CountryCode",
                schema: "ops",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Home_Line1",
                schema: "ops",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Home_Line2",
                schema: "ops",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Home_PostalCode",
                schema: "ops",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Home_Region",
                schema: "ops",
                table: "Customers");
        }
    }
}
