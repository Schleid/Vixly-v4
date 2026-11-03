using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesFeedAndCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RenewalRemindedForUtc",
                schema: "platform",
                table: "Subscriptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutBackgroundColor",
                schema: "platform",
                table: "MerchantAccounts",
                type: "varchar(7)",
                unicode: false,
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutButtonColor",
                schema: "platform",
                table: "MerchantAccounts",
                type: "varchar(7)",
                unicode: false,
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutDisplayName",
                schema: "platform",
                table: "MerchantAccounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutIconUrl",
                schema: "platform",
                table: "MerchantAccounts",
                type: "varchar(500)",
                unicode: false,
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutLogoUrl",
                schema: "platform",
                table: "MerchantAccounts",
                type: "varchar(500)",
                unicode: false,
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatementPrefix",
                schema: "platform",
                table: "MerchantAccounts",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatementSuffix",
                schema: "platform",
                table: "MerchantAccounts",
                type: "varchar(22)",
                unicode: false,
                maxLength: 22,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Delayed",
                schema: "platform",
                table: "CardTransactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Wallet",
                schema: "platform",
                table: "CardTransactions",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SalesFeedEvents",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalsDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SentUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesFeedEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesFeedState",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    LastSuccessUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailingSinceUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastFailure = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    Problem = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    ProblemUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AlertedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesFeedState", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesFeedEvents_OccurredUtc",
                schema: "platform",
                table: "SalesFeedEvents",
                column: "OccurredUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SalesFeedEvents_Sequence",
                schema: "platform",
                table: "SalesFeedEvents",
                column: "Sequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesFeedEvents_Source_SourceId",
                schema: "platform",
                table: "SalesFeedEvents",
                columns: new[] { "Source", "SourceId" },
                unique: true,
                filter: "[SourceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesFeedEvents_Status_Sequence",
                schema: "platform",
                table: "SalesFeedEvents",
                columns: new[] { "Status", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesFeedEvents_TotalsDate",
                schema: "platform",
                table: "SalesFeedEvents",
                column: "TotalsDate",
                unique: true,
                filter: "[TotalsDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesFeedState_Key",
                schema: "platform",
                table: "SalesFeedState",
                column: "Key",
                unique: true);

            // Vixly on the TechCrafters Stripe account (2026-11-02): the statement suffix starts as VIXLY
            // (for example "TECHCRAFT* VIXLY"); staff change it in Control > Payments settings.
            // EXEC defers compiling until the column exists (the idempotent script runs the migration as one batch).
            migrationBuilder.Sql("EXEC(N'UPDATE [platform].[MerchantAccounts] SET [StatementSuffix] = ''VIXLY'' WHERE [StatementSuffix] IS NULL;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesFeedEvents",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "SalesFeedState",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "RenewalRemindedForUtc",
                schema: "platform",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CheckoutBackgroundColor",
                schema: "platform",
                table: "MerchantAccounts");

            migrationBuilder.DropColumn(
                name: "CheckoutButtonColor",
                schema: "platform",
                table: "MerchantAccounts");

            migrationBuilder.DropColumn(
                name: "CheckoutDisplayName",
                schema: "platform",
                table: "MerchantAccounts");

            migrationBuilder.DropColumn(
                name: "CheckoutIconUrl",
                schema: "platform",
                table: "MerchantAccounts");

            migrationBuilder.DropColumn(
                name: "CheckoutLogoUrl",
                schema: "platform",
                table: "MerchantAccounts");

            migrationBuilder.DropColumn(
                name: "StatementPrefix",
                schema: "platform",
                table: "MerchantAccounts");

            migrationBuilder.DropColumn(
                name: "StatementSuffix",
                schema: "platform",
                table: "MerchantAccounts");

            migrationBuilder.DropColumn(
                name: "Delayed",
                schema: "platform",
                table: "CardTransactions");

            migrationBuilder.DropColumn(
                name: "Wallet",
                schema: "platform",
                table: "CardTransactions");
        }
    }
}
