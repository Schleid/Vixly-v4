using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Payments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RefundedTotal",
                schema: "platform",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "MerchantAccounts",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    PublishableKey = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    AcceptedCurrencies = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    CardPaymentsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastCheckedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastCheckSucceeded = table.Column<bool>(type: "bit", nullable: false),
                    LastCheckCode = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MerchantAccounts_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentLinks",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentLinks_Invoices_PlatformInvoiceId",
                        column: x => x.PlatformInvoiceId,
                        principalSchema: "platform",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentLinks_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentLinks_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProviderEvents",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ProviderEventId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ObjectId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    ProcessedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Outcome = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedCards",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ProviderPaymentMethodId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Brand = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Last4 = table.Column<string>(type: "char(4)", unicode: false, fixedLength: true, maxLength: 4, nullable: false),
                    ExpiryMonth = table.Column<int>(type: "int", nullable: false),
                    ExpiryYear = table.Column<int>(type: "int", nullable: false),
                    AddedVia = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    RemovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemovedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedCards_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedCards_Users_RemovedByUserId",
                        column: x => x.RemovedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantCardProfiles",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    ProviderCustomerId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    AutoCharge = table.Column<bool>(type: "bit", nullable: false),
                    ConsentEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    ConsentTextVersion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ConsentUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AutoChargeOffByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AutoChargeOffUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantCardProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantCardProfiles_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantCardProfiles_Users_AutoChargeOffByUserId",
                        column: x => x.AutoChargeOffByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CardTransactions",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Origin = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    ProviderCheckoutId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    ProviderPaymentId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    ProviderRefundId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    SavedCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreditNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChargeTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlatformPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FailureCode = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Dispute = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardTransactions_CardTransactions_ChargeTransactionId",
                        column: x => x.ChargeTransactionId,
                        principalSchema: "platform",
                        principalTable: "CardTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardTransactions_Invoices_PlatformInvoiceId",
                        column: x => x.PlatformInvoiceId,
                        principalSchema: "platform",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardTransactions_SavedCards_SavedCardId",
                        column: x => x.SavedCardId,
                        principalSchema: "platform",
                        principalTable: "SavedCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardTransactions_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardTransactions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_ChargeTransactionId",
                schema: "platform",
                table: "CardTransactions",
                column: "ChargeTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_CreatedByUserId",
                schema: "platform",
                table: "CardTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_IdempotencyKey",
                schema: "platform",
                table: "CardTransactions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_OneOpenChargePerInvoice",
                schema: "platform",
                table: "CardTransactions",
                column: "PlatformInvoiceId",
                unique: true,
                filter: "[Kind] = 1 AND [Status] IN (1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_PlatformInvoiceId_CreatedUtc",
                schema: "platform",
                table: "CardTransactions",
                columns: new[] { "PlatformInvoiceId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_PlatformTenantId_CreatedUtc",
                schema: "platform",
                table: "CardTransactions",
                columns: new[] { "PlatformTenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_ProviderCheckoutId",
                schema: "platform",
                table: "CardTransactions",
                column: "ProviderCheckoutId",
                filter: "[ProviderCheckoutId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_ProviderPaymentId",
                schema: "platform",
                table: "CardTransactions",
                column: "ProviderPaymentId",
                filter: "[ProviderPaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_ProviderRefundId",
                schema: "platform",
                table: "CardTransactions",
                column: "ProviderRefundId",
                filter: "[ProviderRefundId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_SavedCardId",
                schema: "platform",
                table: "CardTransactions",
                column: "SavedCardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_Status_LastModifiedUtc",
                schema: "platform",
                table: "CardTransactions",
                columns: new[] { "Status", "LastModifiedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MerchantAccounts_Key",
                schema: "platform",
                table: "MerchantAccounts",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MerchantAccounts_UpdatedByUserId",
                schema: "platform",
                table: "MerchantAccounts",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLinks_CreatedByUserId",
                schema: "platform",
                table: "PaymentLinks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLinks_PlatformInvoiceId",
                schema: "platform",
                table: "PaymentLinks",
                column: "PlatformInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLinks_PlatformTenantId_Kind",
                schema: "platform",
                table: "PaymentLinks",
                columns: new[] { "PlatformTenantId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLinks_TokenHash",
                schema: "platform",
                table: "PaymentLinks",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderEvents_Provider_ProviderEventId",
                schema: "platform",
                table: "ProviderEvents",
                columns: new[] { "Provider", "ProviderEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedCards_PlatformTenantId_Status",
                schema: "platform",
                table: "SavedCards",
                columns: new[] { "PlatformTenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedCards_ProviderPaymentMethodId",
                schema: "platform",
                table: "SavedCards",
                column: "ProviderPaymentMethodId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedCards_RemovedByUserId",
                schema: "platform",
                table: "SavedCards",
                column: "RemovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantCardProfiles_AutoCharge",
                schema: "platform",
                table: "TenantCardProfiles",
                column: "AutoCharge",
                filter: "[AutoCharge] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TenantCardProfiles_AutoChargeOffByUserId",
                schema: "platform",
                table: "TenantCardProfiles",
                column: "AutoChargeOffByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantCardProfiles_PlatformTenantId",
                schema: "platform",
                table: "TenantCardProfiles",
                column: "PlatformTenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardTransactions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "MerchantAccounts",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PaymentLinks",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "ProviderEvents",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "TenantCardProfiles",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "SavedCards",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "RefundedTotal",
                schema: "platform",
                table: "Invoices");
        }
    }
}
