using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShipmentCap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShipmentCapNoticeLevel",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShipmentCapNoticeMonth",
                schema: "ops",
                table: "TenantSettings",
                type: "varchar(7)",
                unicode: false,
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShipmentGraceMonth",
                schema: "ops",
                table: "TenantSettings",
                type: "varchar(7)",
                unicode: false,
                maxLength: 7,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShipmentCapNoticeLevel",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ShipmentCapNoticeMonth",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ShipmentGraceMonth",
                schema: "ops",
                table: "TenantSettings");
        }
    }
}
