using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Payments;

// =========================================================
// FILE: PlatformZelleClaimConfiguration.cs
//
// PURPOSE:
// Maps customers' Zelle payment reports to platform.ZelleClaims.
//
// RESPONSIBILITIES:
// - One waiting report per invoice (filtered unique index)
// - Indexes for the staff queue and the collections hold
//
// IMPORTANT NOTES:
// - Amounts are money: decimal(18,2); the send date is a date
// =========================================================
internal sealed class PlatformZelleClaimConfiguration : IEntityTypeConfiguration<PlatformZelleClaim>
{
    public void Configure(EntityTypeBuilder<PlatformZelleClaim> builder)
    {
        builder.ToTable("ZelleClaims");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.InvoiceNumber).HasMaxLength(PlatformBillingLimits.NumberMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(item => item.Amount).HasPrecision(18, 2);
        builder.Property(item => item.ReceivedAmount).HasPrecision(18, 2);
        builder.Property(item => item.SentOn).HasColumnType("date");
        builder.Property(item => item.SenderName).HasMaxLength(PlatformZelleLimits.SenderMaxLength).IsRequired();
        builder.Property(item => item.Reference).HasMaxLength(PlatformZelleLimits.ReferenceMaxLength).IsRequired();
        builder.Property(item => item.RejectReason).HasMaxLength(PlatformZelleLimits.ReasonMaxLength);
        builder.Property(item => item.StaffNote).HasMaxLength(PlatformZelleLimits.NoteMaxLength);
        builder.Ignore(item => item.IsWaiting);
        builder.HasIndex(item => item.PlatformInvoiceId, "IX_ZelleClaims_OneWaiting").IsUnique().HasFilter("[Status] = 1");
        builder.HasIndex(item => new { item.Status, item.CreatedUtc });
        builder.HasIndex(item => new { item.PlatformTenantId, item.Status });
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformInvoice>().WithMany().HasForeignKey(item => item.PlatformInvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
