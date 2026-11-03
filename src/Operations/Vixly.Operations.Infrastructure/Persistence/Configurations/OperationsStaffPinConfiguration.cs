using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsStaffPinConfiguration.cs
//
// PURPOSE:
// Maps staff PINs (Slice 12A) to ops.StaffPins.
//
// RESPONSIBILITIES:
// - One PIN per person (unique index)
//
// IMPORTANT NOTES:
// - Only the PIN's hash is stored
// =========================================================
internal sealed class OperationsStaffPinConfiguration : IEntityTypeConfiguration<OperationsStaffPin>
{
    public void Configure(EntityTypeBuilder<OperationsStaffPin> builder)
    {
        builder.ToTable("StaffPins");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.PinHash).HasMaxLength(OperationsIdentityLimits.PasswordHashMaxLength).IsUnicode(false).IsRequired();
        builder.Ignore(item => item.IsLocked);
        builder.HasIndex(item => new { item.TenantId, item.UserId }).IsUnique();
        builder.HasOne<OperationsUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
