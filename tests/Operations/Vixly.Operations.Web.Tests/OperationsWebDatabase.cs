using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Infrastructure.Persistence;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsWebDatabase.cs
//
// PURPOSE:
// A throwaway Operations database for the web tests, built from the migrations.
//
// RESPONSIBILITIES:
// - Create and migrate once per test class
// - Drop it afterwards
//
// IMPORTANT NOTES:
// - Does nothing when no test server is configured
// =========================================================
public sealed class OperationsWebDatabase : IAsyncLifetime
{
    public OperationsWebDatabase()
    {
        string? server = Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable);

        if (!string.IsNullOrWhiteSpace(server))
        {
            ConnectionString = new SqlConnectionStringBuilder(server) { InitialCatalog = "VixlyV4OpsWeb_" + Guid.NewGuid().ToString("N")[..12] }.ConnectionString;
        }
    }

    public string? ConnectionString { get; }

    public async Task InitializeAsync()
    {
        if (ConnectionString is null)
        {
            return;
        }

        await using OperationsDbContext context = NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (ConnectionString is null)
        {
            return;
        }

        await using OperationsDbContext context = NewContext();
        await context.Database.EnsureDeletedAsync();
    }

    private OperationsDbContext NewContext()
    {
        OperationsTenantScope scope = new();
        scope.EnterSystem();
        return new OperationsDbContext(
            new DbContextOptionsBuilder<OperationsDbContext>()
                .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(OperationsDatabase.MigrationsHistoryTable, OperationsDatabase.Schema))
                .Options,
            scope);
    }
}
