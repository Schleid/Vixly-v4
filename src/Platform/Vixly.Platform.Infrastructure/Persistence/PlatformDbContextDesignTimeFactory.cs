using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vixly.Platform.Infrastructure.Persistence;

// =========================================================
// FILE: PlatformDbContextDesignTimeFactory.cs
//
// PURPOSE:
// Lets the EF tools build PlatformDbContext to create
// migrations and bundles without a running host.
//
// RESPONSIBILITIES:
// - Configure SQL Server with the platform history table
//
// IMPORTANT NOTES:
// - Design time only. Creating a migration needs no database,
//   so a placeholder connection string is used unless
//   VIXLY_PLATFORM_DESIGN_CONNECTION is set
// - The default is the owner's development server,
//   GDELL\SQLEXPRESS, database VixlyV4 (shared setup, D-1),
//   with Windows authentication: no secret in the repository
// - Hosts never use this class; they call
//   AddPlatformInfrastructure
// =========================================================
public sealed class PlatformDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable("VIXLY_PLATFORM_DESIGN_CONNECTION")
            ?? @"Server=GDELL\SQLEXPRESS;Database=VixlyV4;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Application Name=Vixly.V4.Platform.Migrations";

        DbContextOptions<PlatformDbContext> options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable(PlatformDatabase.MigrationsHistoryTable, PlatformDatabase.Schema))
            .Options;

        return new PlatformDbContext(options);
    }
}
