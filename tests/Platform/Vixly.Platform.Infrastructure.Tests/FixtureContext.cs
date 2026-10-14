using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Infrastructure.Persistence.Conventions;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: FixtureContext.cs
//
// PURPOSE:
// A test-only DbContext with a parent and a child entity, to
// exercise the D-5 key convention.
//
// RESPONSIBILITIES:
// - Map FixtureCase and its FixtureAction children
// - Apply the convention only when asked
//
// IMPORTANT NOTES:
// - Uses a SQL Server provider that is never connected;
//   model building and change tracking need no database
// - The model is not cached across the two convention modes
// =========================================================
internal sealed class FixtureContext : DbContext
{
    private readonly bool _applyConvention;

    public FixtureContext(bool applyConvention)
    {
        _applyConvention = applyConvention;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlServer("Server=placeholder;Database=Fixture;Integrated Security=true")
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, FixtureModelCacheKeyFactory>();
    }

    internal bool ApplyConvention => _applyConvention;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FixtureCase>(builder =>
        {
            builder.HasKey(entity => entity.Id);
            builder.HasMany(entity => entity.Actions).WithOne().HasForeignKey("CaseId");
            builder.Navigation(entity => entity.Actions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<FixtureAction>().HasKey(entity => entity.Id);

        if (_applyConvention)
        {
            ApplicationAssignedKeyConvention.Apply(modelBuilder);
        }
    }
}
