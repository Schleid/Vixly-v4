using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShippingServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShippingServiceSetups",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCode = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Offered = table.Column<bool>(type: "bit", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    MinDays = table.Column<int>(type: "int", nullable: false),
                    MaxDays = table.Column<int>(type: "int", nullable: false),
                    VolumetricDivisor = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingServiceSetups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingServiceSetups_TenantId",
                schema: "ops",
                table: "ShippingServiceSetups",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingServiceSetups_TenantId_ServiceCode",
                schema: "ops",
                table: "ShippingServiceSetups",
                columns: new[] { "TenantId", "ServiceCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShippingServiceSetups",
                schema: "ops");
        }
    }
}
