using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerWelcomeTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WelcomeTourDismissedUtc",
                schema: "ops",
                table: "Customers",
                type: "datetimeoffset",
                nullable: true);

            // Existing customers may replay from Help; only accounts created after this migration auto-start.
            // Through EXEC so the idempotent script compiles before the column exists (as in PortalPayments).
            migrationBuilder.Sql("EXEC(N'UPDATE [ops].[Customers] SET [WelcomeTourDismissedUtc] = SYSUTCDATETIME();');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WelcomeTourDismissedUtc",
                schema: "ops",
                table: "Customers");
        }
    }
}
