using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CardPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptCard",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Pending",
                schema: "ops",
                table: "Refunds",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "TakenByUserId",
                schema: "ops",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "CardCharges",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    StartedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckoutSessionId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    PaymentIntentId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    PageUrl = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: true),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Live = table.Column<bool>(type: "bit", nullable: false),
                    EndedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardCharges_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "ops",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardCharges_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CardProviders",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecretKeyProtected = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: false),
                    PublishableKey = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    WebhookSecretProtected = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: false),
                    KeyHint = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Live = table.Column<bool>(type: "bit", nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConnectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CardReaders",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    StripeReaderId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardReaders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardReaders_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "ops",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardCharges_InvoiceId_Started",
                schema: "ops",
                table: "CardCharges",
                column: "InvoiceId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CardCharges_TenantId",
                schema: "ops",
                table: "CardCharges",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CardCharges_TenantId_InvoiceId",
                schema: "ops",
                table: "CardCharges",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_CardCharges_WarehouseId",
                schema: "ops",
                table: "CardCharges",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_CardProviders_TenantId",
                schema: "ops",
                table: "CardProviders",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardReaders_TenantId",
                schema: "ops",
                table: "CardReaders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CardReaders_TenantId_StripeReaderId",
                schema: "ops",
                table: "CardReaders",
                columns: new[] { "TenantId", "StripeReaderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardReaders_WarehouseId",
                schema: "ops",
                table: "CardReaders",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardCharges",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "CardProviders",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "CardReaders",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "AcceptCard",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "Pending",
                schema: "ops",
                table: "Refunds");

            migrationBuilder.AlterColumn<Guid>(
                name: "TakenByUserId",
                schema: "ops",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
