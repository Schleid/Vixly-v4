using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Identity;

// =========================================================
// FILE: PlatformDepartmentConfiguration.cs
//
// PURPOSE:
// Maps departments to platform.Departments.
//
// RESPONSIBILITIES:
// - Unique department names
// - Row version for optimistic concurrency
//
// IMPORTANT NOTES:
// - Departments are labels only (decision D-S3)
// =========================================================
internal sealed class PlatformDepartmentConfiguration : IEntityTypeConfiguration<PlatformDepartment>
{
    public void Configure(EntityTypeBuilder<PlatformDepartment> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(department => department.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Property(department => department.Name).HasMaxLength(PlatformIdentityLimits.NameMaxLength).IsRequired();
        builder.Property(department => department.Description).HasMaxLength(PlatformIdentityLimits.DescriptionMaxLength);

        builder.HasIndex(department => department.Name).IsUnique();
    }
}
