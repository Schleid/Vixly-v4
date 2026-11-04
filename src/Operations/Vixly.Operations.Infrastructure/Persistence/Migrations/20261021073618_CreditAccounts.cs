using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreditAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditOverLimit",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "CreditStatements",
                schema: "ops",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "CreditTermsDays",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountDecidedByUserId",
                schema: "ops",
                table: "Invoices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountRequestedByUserId",
                schema: "ops",
                table: "Invoices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AccountRequestedUtc",
                schema: "ops",
                table: "Invoices",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DueOn",
                schema: "ops",
                table: "Invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OnAccount",
                schema: "ops",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CreditAccounts",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Limit = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    TermsDays = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OpenedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatementMonth = table.Column<string>(type: "varchar(7)", unicode: false, maxLength: 7, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditAccounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_OnAccount_CustomerId",
                schema: "ops",
                table: "Invoices",
                columns: new[] { "TenantId", "OnAccount", "CustomerId" },
                filter: "[OnAccount] <> 0");

            migrationBuilder.CreateIndex(
                name: "IX_CreditAccounts_CustomerId",
                schema: "ops",
                table: "CreditAccounts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditAccounts_TenantId",
                schema: "ops",
                table: "CreditAccounts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditAccounts_TenantId_CustomerId",
                schema: "ops",
                table: "CreditAccounts",
                columns: new[] { "TenantId", "CustomerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditAccounts",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_OnAccount_CustomerId",
                schema: "ops",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CreditOverLimit",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "CreditStatements",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "CreditTermsDays",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "AccountDecidedByUserId",
                schema: "ops",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AccountRequestedByUserId",
                schema: "ops",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AccountRequestedUtc",
                schema: "ops",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DueOn",
                schema: "ops",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OnAccount",
                schema: "ops",
                table: "Invoices");
        }
    }
}
