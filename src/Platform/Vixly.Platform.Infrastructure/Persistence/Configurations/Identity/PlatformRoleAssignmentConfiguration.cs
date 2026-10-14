using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Identity;

// =========================================================
// FILE: PlatformRoleAssignmentConfiguration.cs
//
// PURPOSE:
// Maps role grants to platform.RoleAssignments.
//
// RESPONSIBILITIES:
// - Link each grant to its role without cascade
// - Index the lookups by user and by role ("who has this
//   permission")
//
// IMPORTANT NOTES:
// - Grants are never deleted; history stays (V3 behaviour)
// =========================================================
internal sealed class PlatformRoleAssignmentConfiguration : IEntityTypeConfiguration<PlatformRoleAssignment>
{
    public void Configure(EntityTypeBuilder<PlatformRoleAssignment> builder)
    {
        builder.ToTable("RoleAssignments");
        builder.HasKey(assignment => assignment.Id);

        builder.HasOne<PlatformRole>()
            .WithMany()
            .HasForeignKey(assignment => assignment.PlatformRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(assignment => new { assignment.PlatformUserId, assignment.PlatformRoleId });
        builder.HasIndex(assignment => assignment.PlatformRoleId);
    }
}
