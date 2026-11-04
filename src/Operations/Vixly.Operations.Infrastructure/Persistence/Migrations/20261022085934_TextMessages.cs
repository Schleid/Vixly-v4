using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TextMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TextNotices",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NoticeChannel",
                schema: "ops",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NoticeChannelUtc",
                schema: "ops",
                table: "Customers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TextMessages",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Notice = table.Column<int>(type: "int", nullable: false),
                    ToHint = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProviderId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    ErrorCode = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TextProviders",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountSid = table.Column<string>(type: "varchar(34)", unicode: false, maxLength: 34, nullable: false),
                    AuthTokenProtected = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: false),
                    TokenHint = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SmsFrom = table.Column<string>(type: "varchar(34)", unicode: false, maxLength: 34, nullable: true),
                    WhatsAppFrom = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: true),
                    ConnectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CallbackUrl = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Templates = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextProviders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TextMessages_TenantId",
                schema: "ops",
                table: "TextMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TextMessages_TenantId_CreatedUtc",
                schema: "ops",
                table: "TextMessages",
                columns: new[] { "TenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TextMessages_TenantId_ProviderId",
                schema: "ops",
                table: "TextMessages",
                columns: new[] { "TenantId", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_TextProviders_TenantId",
                schema: "ops",
                table: "TextProviders",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TextMessages",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "TextProviders",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "TextNotices",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "NoticeChannel",
                schema: "ops",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NoticeChannelUtc",
                schema: "ops",
                table: "Customers");
        }
    }
}
