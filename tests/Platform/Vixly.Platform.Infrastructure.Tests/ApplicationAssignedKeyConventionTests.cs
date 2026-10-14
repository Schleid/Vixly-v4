using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Vixly.Platform.Infrastructure.Persistence.Conventions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: ApplicationAssignedKeyConventionTests.cs
//
// PURPOSE:
// Proves the D-5 convention changes what EF Core would
// otherwise do with a Guid key.
//
// RESPONSIBILITIES:
// - Without the convention a Guid key is ValueGeneratedOnAdd
// - With it the key is ValueGeneratedNever
// - A child added to a tracked parent is marked Added, so it
//   is inserted, not updated
//
// IMPORTANT NOTES:
// - Uses a small test-only model, because the Platform model
//   has no entities until Slice 1
// =========================================================
public sealed class ApplicationAssignedKeyConventionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void WithoutConvention_GuidKeyIsGeneratedOnAdd()
    {
        using FixtureContext context = new(applyConvention: false);

        Assert.Equal(ValueGenerated.OnAdd, KeyOf<FixtureCase>(context).ValueGenerated);
    }

    [Fact]
    public void WithConvention_GuidKeyIsNeverGenerated()
    {
        using FixtureContext context = new(applyConvention: true);

        Assert.Equal(ValueGenerated.Never, KeyOf<FixtureCase>(context).ValueGenerated);
        Assert.Equal(ValueGenerated.Never, KeyOf<FixtureAction>(context).ValueGenerated);
    }

    [Fact]
    public void WithConvention_ChildAddedToTrackedParent_IsInserted()
    {
        using FixtureContext context = new(applyConvention: true);

        FixtureCase parent = new(Now);
        context.Attach(parent);
        context.ChangeTracker.AcceptAllChanges();

        FixtureAction child = parent.AddAction(Now.AddMinutes(1));
        context.ChangeTracker.DetectChanges();

        Assert.Equal(EntityState.Added, context.Entry(child).State);
    }

    private static IProperty KeyOf<TEntity>(DbContext context)
    {
        return context.Model.FindEntityType(typeof(TEntity))!.FindProperty(nameof(AuditableEntity.Id))!;
    }
}
