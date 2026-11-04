using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PortalPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankTransferInstructionsEn",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankTransferInstructionsEs",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankTransferInstructionsFr",
                schema: "ops",
                table: "TenantSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PortalBankTransfer",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PortalCard",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PortalZelle",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Companies that took cards keep them on the portal, as before (owner, 2026-10-27).
            // EXEC: the column is new in this batch, so the update is compiled only when it runs.
            migrationBuilder.Sql("EXEC(N'UPDATE [ops].[TenantSettings] SET [PortalCard] = [AcceptCard];');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankTransferInstructionsEn",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "BankTransferInstructionsEs",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "BankTransferInstructionsFr",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "PortalBankTransfer",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "PortalCard",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "PortalZelle",
                schema: "ops",
                table: "TenantSettings");
        }
    }
}
