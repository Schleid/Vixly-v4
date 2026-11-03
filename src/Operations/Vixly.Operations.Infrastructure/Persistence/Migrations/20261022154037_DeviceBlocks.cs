using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeviceBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceBlocks",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DeviceNumber = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: true),
                    RegisteredCountry = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    Country = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnblockedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UnblockedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceBlocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceBlocks_TenantId",
                schema: "ops",
                table: "DeviceBlocks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceBlocks_TenantId_CreatedUtc",
                schema: "ops",
                table: "DeviceBlocks",
                columns: new[] { "TenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceBlocks_TenantId_DeviceId",
                schema: "ops",
                table: "DeviceBlocks",
                columns: new[] { "TenantId", "DeviceId" },
                unique: true,
                filter: "[UnblockedUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceBlocks",
                schema: "ops");
        }
    }
}
