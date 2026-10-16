using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Payments;

// =========================================================
// FILE: PlatformZelleAccountConfiguration.cs
//
// PURPOSE:
// Maps Vixly's Zelle details to platform.ZelleAccounts.
//
// RESPONSIBILITIES:
// - One row (unique key), row version, field lengths, the QR image as varbinary
//
// IMPORTANT NOTES:
// - The image is never loaded by the read models except the QR query
// =========================================================
internal sealed class PlatformZelleAccountConfiguration : IEntityTypeConfiguration<PlatformZelleAccount>
{
    public void Configure(EntityTypeBuilder<PlatformZelleAccount> builder)
    {
        builder.ToTable("ZelleAccounts");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Key).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.Key).IsUnique();
        builder.Property(item => item.RecipientName).HasMaxLength(PlatformZelleLimits.RecipientMaxLength).IsRequired();
        builder.Property(item => item.Email).HasMaxLength(PlatformZelleLimits.EmailMaxLength);
        builder.Property(item => item.Tag).HasMaxLength(PlatformZelleLimits.TagMaxLength);
        builder.Property(item => item.Phone).HasMaxLength(PlatformZelleLimits.PhoneMaxLength).IsUnicode(false);
        builder.Property(item => item.QrCode).HasMaxLength(PlatformZelleLimits.MaxQrKilobytes * 1024);
        builder.Property(item => item.QrCodeContentType).HasMaxLength(20).IsUnicode(false);
        builder.Ignore(item => item.HasQrCode);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
