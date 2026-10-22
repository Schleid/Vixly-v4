using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SignInPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "ops",
                table: "UserSessions",
                type: "char(2)",
                unicode: false,
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CountryChanged",
                schema: "ops",
                table: "UserSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                schema: "ops",
                table: "UserSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                schema: "ops",
                table: "UserSessions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceNumber",
                schema: "ops",
                table: "UserSessions",
                type: "varchar(12)",
                unicode: false,
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "ops",
                table: "SignInChallenges",
                type: "char(2)",
                unicode: false,
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CountryChanged",
                schema: "ops",
                table: "SignInChallenges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                schema: "ops",
                table: "SignInChallenges",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                schema: "ops",
                table: "SignInChallenges",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceNumber",
                schema: "ops",
                table: "SignInChallenges",
                type: "varchar(12)",
                unicode: false,
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_TenantId_CreatedUtc",
                schema: "ops",
                table: "UserSessions",
                columns: new[] { "TenantId", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSessions_TenantId_CreatedUtc",
                schema: "ops",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "ops",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "CountryChanged",
                schema: "ops",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                schema: "ops",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                schema: "ops",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "DeviceNumber",
                schema: "ops",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "ops",
                table: "SignInChallenges");

            migrationBuilder.DropColumn(
                name: "CountryChanged",
                schema: "ops",
                table: "SignInChallenges");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                schema: "ops",
                table: "SignInChallenges");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                schema: "ops",
                table: "SignInChallenges");

            migrationBuilder.DropColumn(
                name: "DeviceNumber",
                schema: "ops",
                table: "SignInChallenges");
        }
    }
}
