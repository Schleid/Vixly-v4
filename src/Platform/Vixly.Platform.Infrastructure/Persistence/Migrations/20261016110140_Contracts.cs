using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Contracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublishedEvents",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublishedEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublishedEvents_CreatedUtc",
                schema: "platform",
                table: "PublishedEvents",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PublishedEvents_OutboxMessageId_Type",
                schema: "platform",
                table: "PublishedEvents",
                columns: new[] { "OutboxMessageId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublishedEvents_Sequence",
                schema: "platform",
                table: "PublishedEvents",
                column: "Sequence",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublishedEvents",
                schema: "platform");
        }
    }
}
