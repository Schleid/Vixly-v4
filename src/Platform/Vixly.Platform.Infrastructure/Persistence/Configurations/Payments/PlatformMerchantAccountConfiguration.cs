using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Payments;

// =========================================================
// FILE: PlatformMerchantAccountConfiguration.cs
//
// PURPOSE:
// The merchant account table (one row).
//
// RESPONSIBILITIES:
// - Map PlatformMerchantAccount to its table, keys and indexes
//
// IMPORTANT NOTES:
// - No secret columns (D-P8)
// =========================================================
internal sealed class PlatformMerchantAccountConfiguration : IEntityTypeConfiguration<PlatformMerchantAccount>
{
    public void Configure(EntityTypeBuilder<PlatformMerchantAccount> builder)
    {
        builder.ToTable("MerchantAccounts");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Key).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.Key).IsUnique();
        builder.Property(item => item.PublishableKey).HasMaxLength(PlatformPaymentsLimits.PublishableKeyMaxLength).IsUnicode(false);
        builder.Property(item => item.AcceptedCurrencies).HasMaxLength(PlatformPaymentsLimits.MaxCurrencies * 4).IsUnicode(false).IsRequired();
        builder.Property(item => item.LastCheckCode).HasMaxLength(PlatformPaymentsLimits.FailureCodeMaxLength).IsUnicode(false);
        builder.Ignore(item => item.Currencies);
        builder.Ignore(item => item.IsReady);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
