using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Sales;

// =========================================================
// FILE: PlatformPromotionConfiguration.cs
//
// PURPOSE:
// Maps promotions, their names and eligible plans (D-S20).
//
// RESPONSIBILITIES:
// - Map Promotions, PromotionNames and PromotionPlans
//
// IMPORTANT NOTES:
// - The code is unique; a row version guards redemptions racing for the last use
// =========================================================
internal sealed class PlatformPromotionConfiguration : IEntityTypeConfiguration<PlatformPromotion>
{
    public void Configure(EntityTypeBuilder<PlatformPromotion> builder)
    {
        builder.ToTable("Promotions");
        builder.HasKey(promotion => promotion.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(promotion => promotion.Code).HasMaxLength(PlatformSubscriptionLimits.PlanCodeMaxLength).IsUnicode(false).IsRequired();
        builder.HasIndex(promotion => promotion.Code).IsUnique();
        builder.HasIndex(promotion => promotion.Status);
        builder.Property(promotion => promotion.DecisionReason).HasMaxLength(PlatformSubscriptionLimits.ReasonMaxLength);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(promotion => promotion.DesignedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(promotion => promotion.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(promotion => promotion.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(promotion => promotion.IsEditable);
        builder.OwnsOne(item => item.Terms, terms =>
        {
            terms.Property(item => item.Kind).HasColumnName("DiscountKind");
            terms.Property(item => item.Value).HasColumnName("DiscountValue").HasPrecision(19, 4);
            terms.Property(item => item.Duration).HasColumnName("DiscountDuration");
            terms.Property(item => item.Periods).HasColumnName("DiscountPeriods");
            terms.Property(item => item.EndsUtc).HasColumnName("DiscountEndsUtc");
            terms.Ignore(item => item.HasNoEnd);
        });
        builder.Navigation(promotion => promotion.Terms).IsRequired();

        builder.OwnsMany(promotion => promotion.Names, name =>
        {
            name.ToTable("PromotionNames");
            name.WithOwner().HasForeignKey("PromotionId");
            name.HasKey("PromotionId", nameof(PlatformPromotionName.Language));
            name.Property(item => item.Language).HasMaxLength(10).IsUnicode(false);
            name.Property(item => item.Name).HasMaxLength(PlatformPromotion.NameMaxLength).IsRequired();
        });
        builder.Navigation(promotion => promotion.Names).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(promotion => promotion.Plans, plan =>
        {
            plan.ToTable("PromotionPlans");
            plan.WithOwner().HasForeignKey("PromotionId");
            plan.HasKey("PromotionId", nameof(PlatformPromotionPlan.PlatformPlanId));
            plan.HasOne<PlatformPlan>().WithMany().HasForeignKey(item => item.PlatformPlanId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(promotion => promotion.Plans).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
