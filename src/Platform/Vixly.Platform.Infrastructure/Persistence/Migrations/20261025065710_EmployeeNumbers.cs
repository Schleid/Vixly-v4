using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "EmployeeNumbers",
                schema: "platform");

            // Owner, 2026-10-25: every Control staff member gets TC-0001, TC-0002 ... in the order they were invited;
            // numbers typed by hand before are replaced. The sequence then continues after the last one.
            migrationBuilder.Sql(@"
WITH numbered AS (
    SELECT [Id], ROW_NUMBER() OVER (ORDER BY [CreatedUtc], [Id]) AS [N]
    FROM [platform].[Users]
)
UPDATE u SET [EmployeeNumber] = 'TC-' + RIGHT('0000' + CAST(n.[N] AS varchar(20)), CASE WHEN n.[N] > 9999 THEN LEN(CAST(n.[N] AS varchar(20))) ELSE 4 END)
FROM [platform].[Users] u JOIN numbered n ON n.[Id] = u.[Id];

DECLARE @restart nvarchar(200) = N'ALTER SEQUENCE [platform].[EmployeeNumbers] RESTART WITH '
    + CAST((SELECT COUNT(*) + 1 FROM [platform].[Users]) AS nvarchar(20));
EXEC (@restart);
");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeNumber",
                schema: "platform",
                table: "Users",
                column: "EmployeeNumber",
                unique: true,
                filter: "[EmployeeNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeNumber",
                schema: "platform",
                table: "Users");

            migrationBuilder.DropSequence(
                name: "EmployeeNumbers",
                schema: "platform");
        }
    }
}
