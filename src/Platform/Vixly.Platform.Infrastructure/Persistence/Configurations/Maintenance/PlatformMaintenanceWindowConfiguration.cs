using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Maintenance;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Maintenance;

// =========================================================
// FILE: PlatformMaintenanceWindowConfiguration.cs
//
// PURPOSE:
// Maps maintenance windows.
//
// RESPONSIBILITIES:
// - Table MaintenanceWindows with a row version
//
// IMPORTANT NOTES:
// - Indexed by end for the open windows the sites read every minute
// - The pass hash is unique
// =========================================================
internal sealed class PlatformMaintenanceWindowConfiguration : IEntityTypeConfiguration<PlatformMaintenanceWindow>
{
    public void Configure(EntityTypeBuilder<PlatformMaintenanceWindow> builder)
    {
        builder.ToTable("MaintenanceWindows");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Sites).HasConversion<int>();
        builder.Property(item => item.MessageEn).HasMaxLength(PlatformMaintenanceWindow.MessageMaxLength);
        builder.Property(item => item.MessageFr).HasMaxLength(PlatformMaintenanceWindow.MessageMaxLength);
        builder.Property(item => item.MessageEs).HasMaxLength(PlatformMaintenanceWindow.MessageMaxLength);
        builder.Property(item => item.PassHash).HasMaxLength(PlatformMaintenanceWindow.PassHashMaxLength).IsUnicode(false).IsRequired();
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(item => item.AnnouncedUtc);
        builder.HasIndex(item => item.PassHash).IsUnique();
        builder.HasIndex(item => item.EndsUtc);
    }
}
