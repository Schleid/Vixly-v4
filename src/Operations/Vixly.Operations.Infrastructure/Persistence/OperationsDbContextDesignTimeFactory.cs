using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vixly.Operations.Infrastructure.Persistence;

// =========================================================
// FILE: OperationsDbContextDesignTimeFactory.cs
//
// PURPOSE:
// Builds the DbContext for dotnet ef (migrations only).
//
// RESPONSIBILITIES:
// - Give the tools a context with the ops history table
//
// IMPORTANT NOTES:
// - Never used at run time
// - The connection string comes from VIXLY_OPERATIONS_DESIGN_CONNECTION when set
// =========================================================
public sealed class OperationsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<OperationsDbContext>
{
    public OperationsDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable("VIXLY_OPERATIONS_DESIGN_CONNECTION")
            ?? @"Server=GDELL\SQLEXPRESS;Database=VixlyV4;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Application Name=Vixly.V4.Operations.Migrations";

        DbContextOptions<OperationsDbContext> options = new DbContextOptionsBuilder<OperationsDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable(OperationsDatabase.MigrationsHistoryTable, OperationsDatabase.Schema))
            .Options;

        OperationsTenantScope scope = new();
        scope.EnterSystem();
        return new OperationsDbContext(options, scope);
    }
}
