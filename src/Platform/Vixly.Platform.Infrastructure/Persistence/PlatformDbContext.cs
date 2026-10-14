using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Domain.Audit;
using Vixly.Platform.Domain.Identity;
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
// - Expose the aggregate roots as DbSets (named without the
//   plane prefix, Global Standard 4.1)
// - Store the Data Protection key ring (S-5)
// - Apply the entity configurations in this assembly
// - Apply the application-assigned key convention (D-5)
//
// IMPORTANT NOTES:
// - No foreign key, view or query may reach another schema
//   (D-1)
// - Never migrated at startup; migrations ship as a bundle
//   applied by the pipeline
// =========================================================
public sealed class PlatformDbContext : DbContext, IDataProtectionKeyContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlatformUser> Users => Set<PlatformUser>();

    public DbSet<PlatformRole> Roles => Set<PlatformRole>();

    public DbSet<PlatformDepartment> Departments => Set<PlatformDepartment>();

    public DbSet<PlatformUserSession> UserSessions => Set<PlatformUserSession>();

    public DbSet<PlatformAccountToken> AccountTokens => Set<PlatformAccountToken>();

    public DbSet<PlatformSignInChallenge> SignInChallenges => Set<PlatformSignInChallenge>();

    public DbSet<PlatformPasskeyCeremony> PasskeyCeremonies => Set<PlatformPasskeyCeremony>();

    public DbSet<PlatformPasswordResetRequest> PasswordResetRequests => Set<PlatformPasswordResetRequest>();

    public DbSet<PlatformAuditEntry> AuditEntries => Set<PlatformAuditEntry>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(PlatformDatabase.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);

        ApplicationAssignedKeyConvention.Apply(modelBuilder);
    }
}
