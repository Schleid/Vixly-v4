using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MaintenanceWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaintenanceWindows",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Sites = table.Column<int>(type: "int", nullable: false),
                    NoticeHours = table.Column<int>(type: "int", nullable: false),
                    MessageEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MessageFr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MessageEs = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PassHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndedEarlyUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StoppedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceWindows", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceWindows_EndsUtc",
                schema: "platform",
                table: "MaintenanceWindows",
                column: "EndsUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceWindows_PassHash",
                schema: "platform",
                table: "MaintenanceWindows",
                column: "PassHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenanceWindows",
                schema: "platform");
        }
    }
}
