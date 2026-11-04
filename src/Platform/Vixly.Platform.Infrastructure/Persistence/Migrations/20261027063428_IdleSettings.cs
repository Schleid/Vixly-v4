using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdleSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdleMinutes",
                schema: "platform",
                table: "SecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "IdleWarnMinutes",
                schema: "platform",
                table: "SecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdleMinutes",
                schema: "platform",
                table: "SecuritySettings");

            migrationBuilder.DropColumn(
                name: "IdleWarnMinutes",
                schema: "platform",
                table: "SecuritySettings");
        }
    }
}
