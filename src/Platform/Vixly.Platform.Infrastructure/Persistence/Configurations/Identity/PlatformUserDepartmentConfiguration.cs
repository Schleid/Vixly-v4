using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Identity;

// =========================================================
// FILE: PlatformUserDepartmentConfiguration.cs
//
// PURPOSE:
// Maps department memberships to platform.UserDepartments.
//
// RESPONSIBILITIES:
// - One membership per user and department
// - Link each membership to its department without cascade
//
// IMPORTANT NOTES:
// - "One primary per user" is enforced by PlatformUser
// =========================================================
internal sealed class PlatformUserDepartmentConfiguration : IEntityTypeConfiguration<PlatformUserDepartment>
{
    public void Configure(EntityTypeBuilder<PlatformUserDepartment> builder)
    {
        builder.ToTable("UserDepartments");
        builder.HasKey(membership => membership.Id);

        builder.HasOne<PlatformDepartment>()
            .WithMany()
            .HasForeignKey(membership => membership.PlatformDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(membership => new { membership.PlatformUserId, membership.PlatformDepartmentId }).IsUnique();
        builder.HasIndex(membership => membership.PlatformDepartmentId);
    }
}
