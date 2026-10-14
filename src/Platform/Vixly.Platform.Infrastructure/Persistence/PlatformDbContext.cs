using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Infrastructure.Persistence.Conventions;

namespace Vixly.Platform.Infrastructure.Persistence;

// =========================================================
// FILE: PlatformDbContext.cs
//
// PURPOSE:
// The Platform plane's unit of work over the platform schema.
//
// RESPONSIBILITIES:
// - Place every Platform table in the platform schema
// - Apply the entity configurations in this assembly
// - Apply the application-assigned key convention (D-5)
//
// IMPORTANT NOTES:
// - Entity classes carry the Platform prefix; DbSets and
//   tables do not (Global Standard 4.1): PlatformInvoice maps
//   to platform.Invoices, and each configuration names its
//   table with ToTable
// - No foreign key, view or query may reach another schema
//   (D-1)
// - Never migrated at startup; migrations ship as a bundle
//   applied by the pipeline
// =========================================================
public sealed class PlatformDbContext : DbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(PlatformDatabase.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);

        ApplicationAssignedKeyConvention.Apply(modelBuilder);
    }
}
