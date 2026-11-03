using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Owner, 2026-10-29 (design 4.4): the customer list is now for the whole company only; every role that had it also finds one customer at a time,
            // so staff given such a role for a branch or a warehouse keep finding the customers they serve.
            migrationBuilder.Sql(@"UPDATE [ops].[Roles]
SET [Permissions] = REPLACE([Permissions], N'ops.customers.view', N'ops.customers.view;ops.customers.lookup')
WHERE [IsAdministrator] = 0
  AND (N';' + [Permissions] + N';') LIKE N'%;ops.customers.view;%'
  AND (N';' + [Permissions] + N';') NOT LIKE N'%;ops.customers.lookup;%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
