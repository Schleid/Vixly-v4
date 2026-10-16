using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Infrastructure.Persistence.Conversions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Billing;

// =========================================================
// FILE: PlatformBillingSettingsConfiguration.cs
//
// PURPOSE:
// Maps billing settings (D-B4).
//
// RESPONSIBILITIES:
// - Map the entity to its table, columns and indexes
//
// IMPORTANT NOTES:
// - Single row keyed "default"; row version
// =========================================================
internal sealed class PlatformBillingSettingsConfiguration : IEntityTypeConfiguration<PlatformBillingSettings>
{
    public void Configure(EntityTypeBuilder<PlatformBillingSettings> builder)
    {
        builder.ToTable("BillingSettings");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Key).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.Key).IsUnique();
        builder.Property(item => item.InvoicePrefix).HasMaxLength(PlatformBillingLimits.PrefixMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.CreditNotePrefix).HasMaxLength(PlatformBillingLimits.PrefixMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.ReceiptPrefix).HasMaxLength(PlatformBillingLimits.PrefixMaxLength).IsUnicode(false).IsRequired();
        builder.Ignore(item => item.DueDays);
        builder.HasOne<Vixly.Platform.Domain.Identity.PlatformUser>().WithMany().HasForeignKey(item => item.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsMany(item => item.Texts, text =>
        {
            text.ToTable("BillingTexts");
            text.WithOwner().HasForeignKey("BillingSettingsId");
            text.Property<int>("Id").ValueGeneratedOnAdd();
            text.HasKey("Id");
            text.Property(value => value.Language).HasConversion(PlatformValueConverters.Language).HasMaxLength(8).IsUnicode(false).IsRequired();
            text.Property(value => value.PaymentInstructions).HasMaxLength(PlatformBillingLimits.BillingTextMaxLength);
            text.Property(value => value.Footer).HasMaxLength(PlatformBillingLimits.BillingTextMaxLength);
        });
        builder.Navigation(item => item.Texts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
