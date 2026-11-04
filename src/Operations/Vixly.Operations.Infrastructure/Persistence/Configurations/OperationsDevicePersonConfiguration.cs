using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsDevicePersonConfiguration.cs
//
// PURPOSE:
// Maps the people of each licensed device (Slice 12A) to ops.DevicePeople.
//
// RESPONSIBILITIES:
// - One row per device and person; listed by device, latest first
//
// IMPORTANT NOTES:
// - The device lives in Platform: no foreign key, its id only
// =========================================================
internal sealed class OperationsDevicePersonConfiguration : IEntityTypeConfiguration<OperationsDevicePerson>
{
    public void Configure(EntityTypeBuilder<OperationsDevicePerson> builder)
    {
        builder.ToTable("DevicePeople");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.TenantId, item.DeviceId, item.UserId }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.DeviceId, item.LastSignInUtc });
        builder.HasOne<OperationsUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
