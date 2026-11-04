using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FreightCollect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PayAtDestination",
                schema: "ops",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // The old "allow collect" box meant something else and was never used: freight collect is switched on by the company on purpose (review).
            migrationBuilder.Sql(@"EXEC(N'UPDATE [ops].[TenantSettings] SET [AllowCollect] = 0;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayAtDestination",
                schema: "ops",
                table: "Invoices");
        }
    }
}
