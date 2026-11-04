using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HoldAnswersAndUnclaimed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnclaimedDays",
                schema: "ops",
                table: "TenantSettings",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "RemindersSent",
                schema: "ops",
                table: "Parcels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "HoldAnswers",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParcelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Contents = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DeclaredValue = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: true),
                    Choice = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    DocumentType = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    DocumentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DocumentBytes = table.Column<int>(type: "int", nullable: true),
                    DocumentSha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoldAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoldAnswers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ops",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoldAnswers_Parcels_ParcelId",
                        column: x => x.ParcelId,
                        principalSchema: "ops",
                        principalTable: "Parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HoldAnswers_CustomerId",
                schema: "ops",
                table: "HoldAnswers",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_HoldAnswers_ParcelId",
                schema: "ops",
                table: "HoldAnswers",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_HoldAnswers_TenantId",
                schema: "ops",
                table: "HoldAnswers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HoldAnswers_TenantId_ParcelId_CreatedUtc",
                schema: "ops",
                table: "HoldAnswers",
                columns: new[] { "TenantId", "ParcelId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HoldAnswers_TenantId_ParcelId_Reason_Open",
                schema: "ops",
                table: "HoldAnswers",
                columns: new[] { "TenantId", "ParcelId", "Reason" },
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HoldAnswers",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "UnclaimedDays",
                schema: "ops",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "RemindersSent",
                schema: "ops",
                table: "Parcels");
        }
    }
}
