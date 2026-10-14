using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Sales;

// =========================================================
// FILE: PlatformOfferConfiguration.cs
//
// PURPOSE:
// Maps offer links and their agreed one-time charges (D-S21).
//
// RESPONSIBILITIES:
// - Map Offers and OfferCharges
//
// IMPORTANT NOTES:
// - Only the SHA-256 hash of the link token is stored, unique while set
// - A row version guards a link used twice at once
// =========================================================
internal sealed class PlatformOfferConfiguration : IEntityTypeConfiguration<PlatformOffer>
{
    public void Configure(EntityTypeBuilder<PlatformOffer> builder)
    {
        builder.ToTable("Offers");
        builder.HasKey(offer => offer.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(offer => offer.TokenHash).HasMaxLength(128).IsUnicode(false);
        builder.HasIndex(offer => offer.TokenHash).IsUnique().HasFilter("[TokenHash] IS NOT NULL");
        builder.HasIndex(offer => new { offer.Status, offer.ExpiresUtc });
        builder.Property(offer => offer.BoundEmail).HasMaxLength(254);
        builder.Property(offer => offer.CompanyName).HasMaxLength(PlatformOffer.CompanyMaxLength).IsRequired();
        builder.Property(offer => offer.Reason).HasMaxLength(PlatformSubscriptionLimits.ReasonMaxLength).IsRequired();
        builder.Property(offer => offer.DecisionReason).HasMaxLength(PlatformSubscriptionLimits.ReasonMaxLength);
        builder.Property(offer => offer.SetupFeeOverride).HasPrecision(19, 4);
        builder.HasOne<PlatformPlan>().WithMany().HasForeignKey(offer => offer.PlatformPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(offer => offer.PreparedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(offer => offer.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(offer => offer.RevokedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(offer => offer.IsEditable);
        builder.OwnsOne(item => item.Discount, terms =>
        {
            terms.Property(item => item.Kind).HasColumnName("DiscountKind");
            terms.Property(item => item.Value).HasColumnName("DiscountValue").HasPrecision(19, 4);
            terms.Property(item => item.Duration).HasColumnName("DiscountDuration");
            terms.Property(item => item.Periods).HasColumnName("DiscountPeriods");
            terms.Property(item => item.EndsUtc).HasColumnName("DiscountEndsUtc");
            terms.Ignore(item => item.HasNoEnd);
        });

        builder.OwnsMany(offer => offer.Charges, charge =>
        {
            charge.ToTable("OfferCharges");
            charge.WithOwner().HasForeignKey("OfferId");
            charge.Property<int>("Id").ValueGeneratedOnAdd();
            charge.HasKey("Id");
            charge.Property(item => item.Description).HasMaxLength(PlatformDiscountTerms.ChargeDescriptionMaxLength).IsRequired();
            charge.Property(item => item.Amount).HasPrecision(19, 4);
        });
        builder.Navigation(offer => offer.Charges).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
