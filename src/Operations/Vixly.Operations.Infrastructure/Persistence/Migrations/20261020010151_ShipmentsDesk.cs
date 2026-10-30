using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShipmentsDesk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId_OriginWarehouseId_CreatedUtc",
                schema: "ops",
                table: "Shipments",
                columns: new[] { "TenantId", "OriginWarehouseId", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_TenantId_OriginWarehouseId_CreatedUtc",
                schema: "ops",
                table: "Shipments");
        }
    }
}
