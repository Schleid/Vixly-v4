using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NumberSequences",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastNumber = table.Column<long>(type: "bigint", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    WeightUnit = table.Column<int>(type: "int", nullable: false),
                    DimensionUnit = table.Column<int>(type: "int", nullable: false),
                    AllowCollect = table.Column<bool>(type: "bit", nullable: false),
                    AllowCredit = table.Column<bool>(type: "bit", nullable: false),
                    DeliveryModes = table.Column<int>(type: "int", nullable: false),
                    RequireSecondFactorForAllStaff = table.Column<bool>(type: "bit", nullable: false),
                    ShipmentPrefix = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    ReceivingPrefix = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    InvoicePrefix = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    PaymentPrefix = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_TenantId",
                schema: "ops",
                table: "NumberSequences",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_TenantId_Kind_Year",
                schema: "ops",
                table: "NumberSequences",
                columns: new[] { "TenantId", "Kind", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId",
                schema: "ops",
                table: "TenantSettings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NumberSequences",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "TenantSettings",
                schema: "ops");
        }
    }
}
