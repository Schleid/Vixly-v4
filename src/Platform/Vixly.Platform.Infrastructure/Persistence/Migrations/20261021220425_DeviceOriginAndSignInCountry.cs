using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeviceOriginAndSignInCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "platform",
                table: "UserSessions",
                type: "char(2)",
                unicode: false,
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByName",
                schema: "platform",
                table: "Devices",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredCountry",
                schema: "platform",
                table: "Devices",
                type: "char(2)",
                unicode: false,
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredIp",
                schema: "platform",
                table: "Devices",
                type: "varchar(45)",
                unicode: false,
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredUserAgent",
                schema: "platform",
                table: "Devices",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_CreatedUtc",
                schema: "platform",
                table: "UserSessions",
                column: "CreatedUtc");

            // Who approved the devices already in use: the name kept on the approval in their history (none: the owner's email link).
            migrationBuilder.Sql(@"EXEC(N'UPDATE d SET [ApprovedByName] = e.[ActorName] FROM [platform].[Devices] d CROSS APPLY (SELECT TOP 1 x.[ActorName] FROM [platform].[DeviceEvents] x WHERE x.[PlatformDeviceId] = d.[Id] AND x.[EventCode] = N''platform.device_history.approved'' ORDER BY x.[CreatedUtc] DESC) e WHERE d.[ApprovedByName] IS NULL AND d.[ApprovedUtc] IS NOT NULL');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSessions_CreatedUtc",
                schema: "platform",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "platform",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "ApprovedByName",
                schema: "platform",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "RegisteredCountry",
                schema: "platform",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "RegisteredIp",
                schema: "platform",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "RegisteredUserAgent",
                schema: "platform",
                table: "Devices");
        }
    }
}
