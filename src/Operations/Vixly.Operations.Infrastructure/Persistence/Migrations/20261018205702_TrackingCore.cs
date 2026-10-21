using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackingCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Customers_TenantId_Id",
                schema: "ops",
                table: "Customers",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "Parcels",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Milestone = table.Column<int>(type: "int", nullable: true),
                    Flags = table.Column<int>(type: "int", nullable: false),
                    HoldReasons = table.Column<int>(type: "int", nullable: false),
                    LastEventUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parcels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parcels_Customers_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrackingEvents",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectKind = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ArgumentsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OccurredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TimeZone = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    LocationCode = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    Actor = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    VoidsEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId",
                schema: "ops",
                table: "Parcels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId_CustomerId",
                schema: "ops",
                table: "Parcels",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_TenantId_Milestone",
                schema: "ops",
                table: "Parcels",
                columns: new[] { "TenantId", "Milestone" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_TenantId",
                schema: "ops",
                table: "TrackingEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_TenantId_IdempotencyKey",
                schema: "ops",
                table: "TrackingEvents",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_TenantId_SubjectKind_SubjectId_OccurredUtc",
                schema: "ops",
                table: "TrackingEvents",
                columns: new[] { "TenantId", "SubjectKind", "SubjectId", "OccurredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_TenantId_VoidsEventId",
                schema: "ops",
                table: "TrackingEvents",
                columns: new[] { "TenantId", "VoidsEventId" },
                unique: true,
                filter: "[VoidsEventId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Parcels",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "TrackingEvents",
                schema: "ops");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Customers_TenantId_Id",
                schema: "ops",
                table: "Customers");
        }
    }
}
