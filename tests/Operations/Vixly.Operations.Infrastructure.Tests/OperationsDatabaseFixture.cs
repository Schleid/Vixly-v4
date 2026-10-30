using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Infrastructure.Persistence;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsDatabaseFixture.cs
//
// PURPOSE:
// Creates a throwaway Operations database from the migrations and drops it afterwards.
//
// RESPONSIBILITIES:
// - Apply every migration
// - Add the test tenant table
// - Hand out contexts bound to a tenant scope
//
// IMPORTANT NOTES:
// - Does nothing when no test server is configured
// =========================================================
public sealed class OperationsDatabaseFixture : IAsyncLifetime
{
    private readonly string? _connectionString;

    public OperationsDatabaseFixture()
    {
        string? server = Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable);

        if (!string.IsNullOrWhiteSpace(server))
        {
            _connectionString = new SqlConnectionStringBuilder(server)
            {
                InitialCatalog = "VixlyV4OpsTests_" + Guid.NewGuid().ToString("N")[..12]
            }.ConnectionString;
        }
    }

    public OperationsDbContext NewContext(IOperationsTenantScope scope, bool withParcels = true)
    {
        DbContextOptions<OperationsDbContext> options = new DbContextOptionsBuilder<OperationsDbContext>()
            .UseSqlServer(
                _connectionString ?? throw new InvalidOperationException("No test SQL Server."),
                sql => sql.MigrationsHistoryTable(OperationsDatabase.MigrationsHistoryTable, OperationsDatabase.Schema))
            .ReplaceService<IModelCacheKeyFactory, FixtureModelCacheKeyFactory>()
            .Options;

        return withParcels
            ? new OperationsDbContext(options, scope, model => model.Entity<FixtureParcel>(parcel =>
            {
                parcel.ToTable("FixtureParcels");
                parcel.HasKey(entity => entity.Id);
                parcel.Property(entity => entity.Label).HasMaxLength(50);
            }))
            : new OperationsDbContext(options, scope);
    }

    public OperationsDbContext NewContext(Guid tenantId)
    {
        OperationsTenantScope scope = new();
        scope.Enter(tenantId);
        return NewContext(scope);
    }

    public OperationsDbContext NewSystemContext(bool withParcels = true)
    {
        OperationsTenantScope scope = new();
        scope.EnterSystem();
        return NewContext(scope, withParcels);
    }

    public async Task InitializeAsync()
    {
        if (_connectionString is null)
        {
            return;
        }

        await using OperationsDbContext context = NewSystemContext(withParcels: false);
        await context.Database.MigrateAsync();
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE ops.FixtureParcels (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_FixtureParcels PRIMARY KEY,
                TenantId uniqueidentifier NOT NULL,
                Label nvarchar(50) NOT NULL,
                CreatedUtc datetimeoffset NOT NULL,
                LastModifiedUtc datetimeoffset NULL);
            """);
    }

    public async Task DisposeAsync()
    {
        if (_connectionString is null)
        {
            return;
        }

        await using OperationsDbContext context = NewSystemContext(withParcels: false);
        await context.Database.EnsureDeletedAsync();
    }
}
