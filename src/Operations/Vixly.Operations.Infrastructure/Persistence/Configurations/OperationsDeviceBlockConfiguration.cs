using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsDeviceBlockConfiguration.cs
//
// PURPOSE:
// Maps devices blocked after a sign-in from another country (owner, R-6) to ops.DeviceBlocks.
//
// RESPONSIBILITIES:
// - One open block per device (unique filtered index)
//
// IMPORTANT NOTES:
// - The device lives in Platform: no foreign key, its id only
// =========================================================
internal sealed class OperationsDeviceBlockConfiguration : IEntityTypeConfiguration<OperationsDeviceBlock>
{
    public void Configure(EntityTypeBuilder<OperationsDeviceBlock> builder)
    {
        builder.ToTable("DeviceBlocks");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.DeviceName).HasMaxLength(OperationsIdentityLimits.DeviceNameMaxLength);
        builder.Property(item => item.DeviceNumber).HasMaxLength(OperationsIdentityLimits.DeviceNumberMaxLength).IsUnicode(false);
        builder.Property(item => item.RegisteredCountry).HasMaxLength(2).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(item => item.Country).HasMaxLength(2).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Ignore(item => item.IsOpen);
        builder.HasIndex(item => new { item.TenantId, item.DeviceId }).IsUnique().HasFilter("[UnblockedUtc] IS NULL");
        builder.HasIndex(item => new { item.TenantId, item.CreatedUtc });
    }
}
