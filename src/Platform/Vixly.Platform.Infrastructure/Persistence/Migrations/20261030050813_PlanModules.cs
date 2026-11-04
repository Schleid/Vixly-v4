using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vixly.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Vixly Lite (owner, 2026-10-30): every plan version that exists keeps the seven Operations modules; a Lite plan is one without them.
            migrationBuilder.Sql(@"INSERT INTO [platform].[PlanVersionFeatures] ([PlanVersionId], [Feature])
SELECT v.[Id], m.[Feature]
FROM [platform].[PlanVersions] AS v
CROSS JOIN (VALUES (12), (13), (14), (15), (16), (17), (18)) AS m([Feature])
WHERE NOT EXISTS (SELECT 1 FROM [platform].[PlanVersionFeatures] AS f WHERE f.[PlanVersionId] = v.[Id] AND f.[Feature] = m.[Feature]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [platform].[PlanVersionFeatures] WHERE [Feature] BETWEEN 12 AND 18;");
        }
    }
}
