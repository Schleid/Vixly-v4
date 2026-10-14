using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PasskeyCeremonies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasskeyCeremonies",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    PlatformUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OptionsJson = table.Column<string>(type: "varchar(max)", unicode: false, maxLength: 16000, nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasskeyCeremonies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasskeyCeremonies_Users_PlatformUserId",
                        column: x => x.PlatformUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyCeremonies_ExpiresUtc",
                schema: "platform",
                table: "PasskeyCeremonies",
                column: "ExpiresUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyCeremonies_PlatformUserId",
                schema: "platform",
                table: "PasskeyCeremonies",
                column: "PlatformUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyCeremonies_TokenHash",
                schema: "platform",
                table: "PasskeyCeremonies",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasskeyCeremonies",
                schema: "platform");
        }
    }
}
