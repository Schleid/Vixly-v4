using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformDatabaseFixture.cs
//
// PURPOSE:
// Creates a throwaway Platform database from the migrations for
// database tests, and drops it afterwards.
//
// RESPONSIBILITIES:
// - Build a unique database name
// - Apply every migration once per test class
// - Add one staff user to act as creator
// - Hand out fresh contexts
//
// IMPORTANT NOTES:
// - Does nothing when no test server is configured
// =========================================================
public sealed class PlatformDatabaseFixture : IAsyncLifetime
{
    private readonly string? _connectionString;

    public PlatformDatabaseFixture()
    {
        string? server = Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable);

        if (!string.IsNullOrWhiteSpace(server))
        {
            _connectionString = new SqlConnectionStringBuilder(server)
            {
                InitialCatalog = "VixlyV4Tests_" + Guid.NewGuid().ToString("N")[..12]
            }.ConnectionString;
        }
    }

    public PlatformDbContext NewContext()
    {
        DbContextOptions<PlatformDbContext> options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer(
                _connectionString ?? throw new InvalidOperationException("No test SQL Server."),
                sql => sql.MigrationsHistoryTable(PlatformDatabase.MigrationsHistoryTable, PlatformDatabase.Schema))
            .Options;

        return new PlatformDbContext(options);
    }

    public Guid ActorUserId { get; private set; }

    public async Task InitializeAsync()
    {
        if (_connectionString is null)
        {
            return;
        }

        await using PlatformDbContext context = NewContext();
        await context.Database.MigrateAsync();

        PlatformUser actor = PlatformUser.Invite(
            "Test",
            "Actor",
            EmailAddress.Create($"actor-{Guid.NewGuid():N}@techcrafters.example"),
            LanguageCode.En,
            PlatformUserAuthorityLevel.SuperUser,
            null,
            null,
            DateTimeOffset.UtcNow);
        context.Users.Add(actor);
        await context.SaveChangesAsync();
        ActorUserId = actor.Id;
    }

    public async Task DisposeAsync()
    {
        if (_connectionString is null)
        {
            return;
        }

        await using PlatformDbContext context = NewContext();
        await context.Database.EnsureDeletedAsync();
    }
}
