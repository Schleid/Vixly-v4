using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomersMailbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMailbox",
                schema: "ops",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId_Mailbox",
                schema: "ops",
                table: "Warehouses",
                columns: new[] { "TenantId", "IsMailbox" },
                unique: true,
                filter: "[IsMailbox] = 1");

            // Owner, 2026-10-21: each company's oldest active receiving warehouse that accepts deliveries becomes its customers' mailbox.
            // EXEC: the script is compiled per batch, so the new column is only named once it exists (upgrade test of package 94).
            migrationBuilder.Sql(@"
EXEC(N'UPDATE w SET [IsMailbox] = 1
FROM [ops].[Warehouses] w
WHERE w.[Id] = (SELECT TOP (1) c.[Id] FROM [ops].[Warehouses] c
                WHERE c.[TenantId] = w.[TenantId] AND c.[Status] = 0 AND c.[AcceptingDeliveries] = 1 AND (c.[Roles] & 1) <> 0
                ORDER BY c.[CreatedUtc], c.[Code])
  AND NOT EXISTS (SELECT 1 FROM [ops].[Warehouses] m WHERE m.[TenantId] = w.[TenantId] AND m.[IsMailbox] = 1);');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Warehouses_TenantId_Mailbox",
                schema: "ops",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "IsMailbox",
                schema: "ops",
                table: "Warehouses");
        }
    }
}
