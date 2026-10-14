using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Platform.Infrastructure.DependencyInjection;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformDbContextTests.cs
//
// PURPOSE:
// Proves the Platform data placement rules of Global Standard
// D-1 and D-5 without a database.
//
// RESPONSIBILITIES:
// - Default schema is platform
// - Migrations history table lives in the platform schema
// - Registration reads ConnectionStrings:Platform and fails
//   fast without it
// - Every AuditableEntity in the model has a key that the
//   database never generates
//
// IMPORTANT NOTES:
// - Building the model and the history script needs no
//   connection, so these run in CI without SQL Server
// - The connection string below is a placeholder that is
//   never opened
// =========================================================
public sealed class PlatformDbContextTests
{
    private const string PlaceholderConnection = "Server=placeholder;Database=VixlyV4;Integrated Security=true";

    [Fact]
    public void Model_UsesThePlatformSchema()
    {
        using PlatformDbContext context = CreateFromServices();

        Assert.Equal("platform", context.Model.GetDefaultSchema());
    }

    [Fact]
    public void MigrationsHistory_LivesInThePlatformSchema()
    {
        using PlatformDbContext context = CreateFromServices();

        string script = context.GetService<IHistoryRepository>().GetCreateIfNotExistsScript();

        Assert.Contains("[platform].[__EFMigrationsHistory]", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Registration_FailsWithoutThePlatformConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddPlatformInfrastructure(configuration));

        Assert.Contains("ConnectionStrings:Platform", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryAuditableEntity_HasAnApplicationAssignedKey()
    {
        using PlatformDbContext context = CreateFromServices();

        foreach (IEntityType entityType in context.Model.GetEntityTypes()
            .Where(type => typeof(AuditableEntity).IsAssignableFrom(type.ClrType)))
        {
            IProperty id = entityType.FindProperty(nameof(AuditableEntity.Id))!;

            Assert.True(
                id.ValueGenerated == ValueGenerated.Never,
                $"{entityType.ClrType.Name}.Id must be ValueGeneratedNever (Global Standard D-5).");
        }
    }

    private static PlatformDbContext CreateFromServices()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Platform"] = PlaceholderConnection
            })
            .Build();

        ServiceProvider provider = new ServiceCollection()
            .AddPlatformInfrastructure(configuration)
            .BuildServiceProvider();

        return provider.GetRequiredService<PlatformDbContext>();
    }
}
