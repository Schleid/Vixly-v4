using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Sales;

// =========================================================
// FILE: PlatformSalesSettingsConfiguration.cs
//
// PURPOSE:
// Maps the Sales settings and their fixed-amount limits (D-S22).
//
// RESPONSIBILITIES:
// - Map SalesSettings and SalesFixedLimits
//
// IMPORTANT NOTES:
// - One row: a unique key column guarantees it
// - Every value is nullable; nothing is preset
// =========================================================
internal sealed class PlatformSalesSettingsConfiguration : IEntityTypeConfiguration<PlatformSalesSettings>
{
    public void Configure(EntityTypeBuilder<PlatformSalesSettings> builder)
    {
        builder.ToTable("SalesSettings");
        builder.HasKey(settings => settings.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(settings => settings.Key).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(settings => settings.Key).IsUnique();
        builder.Property(settings => settings.MaxPercentWithoutApproval).HasPrecision(5, 2);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(settings => settings.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(settings => settings.FixedLimits, limit =>
        {
            limit.ToTable("SalesFixedLimits");
            limit.WithOwner().HasForeignKey("SalesSettingsId");
            limit.HasKey("SalesSettingsId", nameof(PlatformSalesFixedLimit.CurrencyCode));
            limit.Property(item => item.CurrencyCode).HasMaxLength(PlatformSubscriptionLimits.CurrencyCodeLength).IsFixedLength().IsUnicode(false);
            limit.Property(item => item.Amount).HasPrecision(19, 4);
        });
        builder.Navigation(settings => settings.FixedLimits).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
