using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Infrastructure.Persistence.Conversions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Sales;

// =========================================================
// FILE: PlatformSalesLeadConfiguration.cs
//
// PURPOSE:
// Maps contact requests (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Table SalesLeads
//
// IMPORTANT NOTES:
// - Visitor text is nvarchar; the IP is kept for abuse checks and never shown
// =========================================================
internal sealed class PlatformSalesLeadConfiguration : IEntityTypeConfiguration<PlatformSalesLead>
{
    public void Configure(EntityTypeBuilder<PlatformSalesLead> builder)
    {
        builder.ToTable("SalesLeads");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.CompanyName).HasMaxLength(PlatformSalesLeadLimits.CompanyMaxLength).IsRequired();
        builder.Property(item => item.ContactName).HasMaxLength(PlatformSalesLeadLimits.NameMaxLength).IsRequired();
        builder.Property(item => item.Email).HasMaxLength(254).IsRequired();
        builder.Property(item => item.Phone).HasMaxLength(PlatformSalesLeadLimits.PhoneMaxLength).IsUnicode(false);
        builder.Property(item => item.CountryCode).HasMaxLength(2).IsUnicode(false).IsFixedLength();
        builder.Property(item => item.Message).HasMaxLength(PlatformSalesLeadLimits.MessageMaxLength);
        builder.Property(item => item.Language).HasConversion(PlatformValueConverters.Language).HasMaxLength(8).IsUnicode(false).IsRequired();
        builder.Property(item => item.SourcePage).HasMaxLength(PlatformSalesLeadLimits.SourceMaxLength);
        builder.Property(item => item.ClientIp).HasMaxLength(PlatformSalesLeadLimits.IpAddressMaxLength).IsUnicode(false);
        builder.Property(item => item.StaffNote).HasMaxLength(PlatformSalesLeadLimits.NoteMaxLength);
        builder.HasIndex(item => new { item.Status, item.CreatedUtc });
        builder.HasIndex(item => new { item.Status, item.StatusChangedUtc });
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.HandledByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
