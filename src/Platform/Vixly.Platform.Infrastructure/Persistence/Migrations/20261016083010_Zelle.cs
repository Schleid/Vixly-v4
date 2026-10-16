using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Zelle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZelleAccounts",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    QrCode = table.Column<byte[]>(type: "varbinary(max)", maxLength: 262144, nullable: true),
                    QrCodeContentType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    QrCodeVersion = table.Column<int>(type: "int", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZelleAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZelleAccounts_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ZelleClaims",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SentOn = table.Column<DateTime>(type: "date", nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ReceivedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PlatformPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StaffNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZelleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZelleClaims_Invoices_PlatformInvoiceId",
                        column: x => x.PlatformInvoiceId,
                        principalSchema: "platform",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZelleClaims_Tenants_PlatformTenantId",
                        column: x => x.PlatformTenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZelleClaims_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalSchema: "platform",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZelleAccounts_Key",
                schema: "platform",
                table: "ZelleAccounts",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZelleAccounts_UpdatedByUserId",
                schema: "platform",
                table: "ZelleAccounts",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ZelleClaims_DecidedByUserId",
                schema: "platform",
                table: "ZelleClaims",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ZelleClaims_OneWaiting",
                schema: "platform",
                table: "ZelleClaims",
                column: "PlatformInvoiceId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ZelleClaims_PlatformTenantId_Status",
                schema: "platform",
                table: "ZelleClaims",
                columns: new[] { "PlatformTenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ZelleClaims_Status_CreatedUtc",
                schema: "platform",
                table: "ZelleClaims",
                columns: new[] { "Status", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZelleAccounts",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "ZelleClaims",
                schema: "platform");
        }
    }
}
