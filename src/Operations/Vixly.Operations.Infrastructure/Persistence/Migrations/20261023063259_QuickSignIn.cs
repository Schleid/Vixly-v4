using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QuickSignIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsQuick",
                schema: "ops",
                table: "UserSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DevicePeople",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastSignInUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastFullSignInUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevicePeople", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevicePeople_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "ops",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StaffBadges",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevokedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffBadges_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "ops",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StaffPins",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PinHash = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Failures = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffPins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "ops",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevicePeople_TenantId",
                schema: "ops",
                table: "DevicePeople",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePeople_TenantId_DeviceId_LastSignInUtc",
                schema: "ops",
                table: "DevicePeople",
                columns: new[] { "TenantId", "DeviceId", "LastSignInUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DevicePeople_TenantId_DeviceId_UserId",
                schema: "ops",
                table: "DevicePeople",
                columns: new[] { "TenantId", "DeviceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevicePeople_UserId",
                schema: "ops",
                table: "DevicePeople",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffBadges_CodeHash",
                schema: "ops",
                table: "StaffBadges",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffBadges_TenantId",
                schema: "ops",
                table: "StaffBadges",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffBadges_TenantId_UserId",
                schema: "ops",
                table: "StaffBadges",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "[RevokedUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StaffBadges_UserId",
                schema: "ops",
                table: "StaffBadges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPins_TenantId",
                schema: "ops",
                table: "StaffPins",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPins_TenantId_UserId",
                schema: "ops",
                table: "StaffPins",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffPins_UserId",
                schema: "ops",
                table: "StaffPins",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevicePeople",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "StaffBadges",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "StaffPins",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "IsQuick",
                schema: "ops",
                table: "UserSessions");
        }
    }
}
