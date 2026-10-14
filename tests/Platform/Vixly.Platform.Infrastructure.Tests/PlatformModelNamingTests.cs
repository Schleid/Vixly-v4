using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Vixly.Platform.Infrastructure.Persistence;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformModelNamingTests.cs
//
// PURPOSE:
// Proves the Platform model follows the naming and placement
// rules (Global Standard 4.1 and D-1).
//
// RESPONSIBILITIES:
// - Every table is in the platform schema
// - No table name repeats the plane prefix
// - Every entity class carries the Platform prefix, except
//   the framework's DataProtectionKey
// - The model is not empty (the scan reaches the entities)
//
// IMPORTANT NOTES:
// - Builds the model only; no database is opened
// =========================================================
public sealed class PlatformModelNamingTests
{
    [Fact]
    public void EveryTable_IsInThePlatformSchema_WithoutThePrefix()
    {
        using PlatformDbContext context = new PlatformDbContextDesignTimeFactory().CreateDbContext(Array.Empty<string>());

        IEntityType[] entityTypes = context.Model.GetEntityTypes().ToArray();

        Assert.NotEmpty(entityTypes);

        foreach (IEntityType entityType in entityTypes)
        {
            string table = entityType.GetTableName()!;

            Assert.Equal("platform", entityType.GetSchema());
            Assert.False(
                table.StartsWith("Platform", StringComparison.Ordinal),
                $"{entityType.ClrType.Name} maps to '{table}'; tables drop the plane prefix (Global Standard 4.1).");
            if (entityType.ClrType != typeof(DataProtectionKey))
            {
                Assert.StartsWith("Platform", entityType.ClrType.Name, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Model_HasNoPendingChanges_AgainstTheLastMigration()
    {
        using PlatformDbContext context = new PlatformDbContextDesignTimeFactory().CreateDbContext(Array.Empty<string>());

        Assert.False(context.Database.HasPendingModelChanges(), "Run 'dotnet ef migrations add' for the model change.");
    }
}
