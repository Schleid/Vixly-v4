using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Infrastructure.Persistence;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsModelTests.cs
//
// PURPOSE:
// Proves the Operations model rules without a database (Slice 0, D-F1, D-F8).
//
// RESPONSIBILITIES:
// - Every table lives in the ops schema
// - Every tenant entity has the tenant filter and index
//
// IMPORTANT NOTES:
// - Builds the model only
// =========================================================
public sealed class OperationsModelTests
{
    [Fact]
    public void EveryTable_IsInTheOpsSchema()
    {
        using OperationsDbContext context = NewContext();

        foreach (IEntityType entity in context.Model.GetEntityTypes())
        {
            Assert.Equal(OperationsDatabase.Schema, entity.GetSchema());
        }
    }

    [Fact]
    public void TenantEntities_HaveTheFilterAndIndex()
    {
        using OperationsDbContext context = NewContext();
        IEntityType parcel = context.Model.FindEntityType(typeof(FixtureParcel))!;

        Assert.NotEmpty(parcel.GetDeclaredQueryFilters());
        Assert.Contains(parcel.GetIndexes(), index => index.Properties.Single().Name == nameof(OperationsEntity.TenantId));
    }

    [Fact]
    public void Keys_AreAssignedByTheApplication()
    {
        using OperationsDbContext context = NewContext();

        foreach (IEntityType entity in context.Model.GetEntityTypes().Where(type => type.FindProperty("Id") is not null && type.ClrType != typeof(Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey)))
        {
            Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never, entity.FindProperty("Id")!.ValueGenerated);
        }
    }

    private static OperationsDbContext NewContext()
    {
        OperationsTenantScope scope = new();
        scope.EnterSystem();
        DbContextOptions<OperationsDbContext> options = new DbContextOptionsBuilder<OperationsDbContext>()
            .UseSqlServer("Server=placeholder;Database=Model;Integrated Security=true")
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, FixtureModelCacheKeyFactory>()
            .Options;

        return new OperationsDbContext(options, scope, model => model.Entity<FixtureParcel>(parcel => parcel.ToTable("FixtureParcels")));
    }
}
