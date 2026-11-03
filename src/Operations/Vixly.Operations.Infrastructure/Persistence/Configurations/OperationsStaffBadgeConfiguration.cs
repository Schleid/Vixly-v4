using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsStaffBadgeConfiguration.cs
//
// PURPOSE:
// Maps staff badges (Slice 12A) to ops.StaffBadges.
//
// RESPONSIBILITIES:
// - One badge in use per person (unique filtered index); codes unique
//
// IMPORTANT NOTES:
// - Only the code's hash is stored
// =========================================================
internal sealed class OperationsStaffBadgeConfiguration : IEntityTypeConfiguration<OperationsStaffBadge>
{
    public void Configure(EntityTypeBuilder<OperationsStaffBadge> builder)
    {
        builder.ToTable("StaffBadges");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.CodeHash).HasMaxLength(OperationsIdentityLimits.TokenHashLength).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Ignore(item => item.IsActive);
        builder.HasIndex(item => item.CodeHash).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.UserId }).IsUnique().HasFilter("[RevokedUtc] IS NULL");
        builder.HasOne<OperationsUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
