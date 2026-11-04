using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OperationalEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CausedByEventId",
                schema: "ops",
                table: "TrackingEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisabledEventCauses",
                schema: "ops",
                table: "TenantSettings",
                type: "varchar(400)",
                unicode: false,
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EventEmails",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "EventReasons",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    TextEn = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TextFr = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    TextEs = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventReasons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_TenantId_CausedByEventId",
                schema: "ops",
                table: "TrackingEvents",
                columns: new[] { "TenantId", "CausedByEventId" },
                filter: "[CausedByEventId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventReasons_TenantId",
                schema: "ops",
                table: "EventReasons",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EventReasons_TenantId_Code",
                schema: "ops",
                table: "EventReasons",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventReasons",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_TrackingEvents_TenantId_CausedByEventId",
                schema: "ops",
                table: "TrackingEvents");

            migrationBuilder.DropColumn(
                name: "CausedByEventId",
                schema: "ops",
                table: "TrackingEvents");

            migrationBuilder.DropColumn(
                name: "DisabledEventCauses",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "EventEmails",
                schema: "ops",
                table: "TenantSettings");
        }
    }
}
