using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CounterSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerSignatureKey",
                schema: "ops",
                table: "Shipments",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerSignatureRecordedByUserId",
                schema: "ops",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CustomerSignedUtc",
                schema: "ops",
                table: "Shipments",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerSignatureKey",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CustomerSignatureRecordedByUserId",
                schema: "ops",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CustomerSignedUtc",
                schema: "ops",
                table: "Shipments");
        }
    }
}
