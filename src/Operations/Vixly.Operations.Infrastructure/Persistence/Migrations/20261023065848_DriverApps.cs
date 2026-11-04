using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DriverApps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CodeUnconfirmed",
                schema: "ops",
                table: "DeliveryStops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TrackTokenHash",
                schema: "ops",
                table: "DeliveryStops",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DriverPositions",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    AccuracyMeters = table.Column<int>(type: "int", nullable: false),
                    TakenUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverPositions_DeliveryRoutes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "ops",
                        principalTable: "DeliveryRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryStops_TrackTokenHash",
                schema: "ops",
                table: "DeliveryStops",
                column: "TrackTokenHash",
                unique: true,
                filter: "[TrackTokenHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DriverPositions_RouteId",
                schema: "ops",
                table: "DriverPositions",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverPositions_TakenUtc",
                schema: "ops",
                table: "DriverPositions",
                column: "TakenUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DriverPositions_TenantId",
                schema: "ops",
                table: "DriverPositions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverPositions_TenantId_RouteId_TakenUtc",
                schema: "ops",
                table: "DriverPositions",
                columns: new[] { "TenantId", "RouteId", "TakenUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverPositions",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryStops_TrackTokenHash",
                schema: "ops",
                table: "DeliveryStops");

            migrationBuilder.DropColumn(
                name: "CodeUnconfirmed",
                schema: "ops",
                table: "DeliveryStops");

            migrationBuilder.DropColumn(
                name: "TrackTokenHash",
                schema: "ops",
                table: "DeliveryStops");
        }
    }
}
