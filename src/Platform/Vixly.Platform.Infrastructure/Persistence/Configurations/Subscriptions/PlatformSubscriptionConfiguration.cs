using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionConfiguration.cs
//
// PURPOSE:
// Maps tenant subscriptions and their scheduled change.
//
// RESPONSIBILITIES:
// - Map Subscriptions with the scheduled change in the same row
//
// IMPORTANT NOTES:
// - One current subscription per tenant: filtered unique index on the tenant where the status is not Ended (D-S3)
// - A row version makes the clock job handle each subscription once
// =========================================================
internal sealed class PlatformSubscriptionConfiguration : IEntityTypeConfiguration<PlatformSubscription>
{
    public void Configure(EntityTypeBuilder<PlatformSubscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(subscription => subscription.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasIndex(subscription => subscription.PlatformTenantId, "IX_Subscriptions_OneCurrentPerTenant")
            .IsUnique()
            .HasFilter("[Status] <> 5");
        builder.HasIndex(subscription => new { subscription.Status, subscription.PeriodEndUtc });
        builder.HasIndex(subscription => new { subscription.Status, subscription.TrialEndsUtc });
        builder.HasIndex(subscription => new { subscription.PlatformPlanId, subscription.Status });
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(subscription => subscription.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformPlan>().WithMany().HasForeignKey(subscription => subscription.PlatformPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformPlanVersion>().WithMany().HasForeignKey(subscription => subscription.PlanVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(subscription => subscription.IsCurrent);
        builder.Ignore(subscription => subscription.ActiveDiscount);
        builder.Ignore(subscription => subscription.PendingDiscount);
        builder.Ignore(subscription => subscription.PendingCharges);
        builder.Property(subscription => subscription.SetupFeeOverride).HasPrecision(19, 4);

        builder.OwnsMany(subscription => subscription.Discounts, discount =>
        {
            discount.ToTable("SubscriptionDiscounts");
            discount.WithOwner().HasForeignKey(item => item.PlatformSubscriptionId);
            discount.HasKey(item => item.Id);
            discount.Property(item => item.Id).ValueGeneratedNever();
            discount.Property(item => item.Reason).HasMaxLength(PlatformSubscriptionLimits.ReasonMaxLength).IsRequired();
            discount.Property(item => item.DecisionReason).HasMaxLength(PlatformSubscriptionLimits.ReasonMaxLength);
            // "One active discount" is kept by the aggregate and the Subscriptions row version, not an index:
            // replacing a discount updates two rows, and SQL Server checks a unique index after each statement.
            discount.HasIndex(item => new { item.PlatformSubscriptionId, item.Status });
            discount.HasIndex(item => item.PlatformSubscriptionId, "IX_SubscriptionDiscounts_OnePending").IsUnique().HasFilter("[Status] = 0");
            discount.HasIndex(item => new { item.Source, item.SourceId });
            discount.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.GrantedByUserId).OnDelete(DeleteBehavior.Restrict);
            discount.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
            discount.Ignore(item => item.IsActive);
            discount.Ignore(item => item.IsPending);
            discount.OwnsOne(item => item.Terms, terms =>
            {
                terms.Property(item => item.Kind).HasColumnName("DiscountKind");
                terms.Property(item => item.Value).HasColumnName("DiscountValue").HasPrecision(19, 4);
                terms.Property(item => item.Duration).HasColumnName("DiscountDuration");
                terms.Property(item => item.Periods).HasColumnName("DiscountPeriods");
                terms.Property(item => item.EndsUtc).HasColumnName("DiscountEndsUtc");
                terms.Ignore(item => item.HasNoEnd);
            });
            discount.Navigation(item => item.Terms).IsRequired();
        });
        builder.Navigation(subscription => subscription.Discounts).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(subscription => subscription.Charges, charge =>
        {
            charge.ToTable("OneTimeCharges");
            charge.WithOwner().HasForeignKey(item => item.PlatformSubscriptionId);
            charge.HasKey(item => item.Id);
            charge.Property(item => item.Id).ValueGeneratedNever();
            charge.Property(item => item.Description).HasMaxLength(PlatformDiscountTerms.ChargeDescriptionMaxLength).IsRequired();
            charge.Property(item => item.Amount).HasPrecision(19, 4);
            charge.Property(item => item.CurrencyCode).HasMaxLength(PlatformSubscriptionLimits.CurrencyCodeLength).IsFixedLength().IsUnicode(false).IsRequired();
            charge.Property(item => item.Reason).HasMaxLength(PlatformSubscriptionLimits.ReasonMaxLength).IsRequired();
            charge.HasIndex(item => item.Status);
            charge.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.AddedByUserId).OnDelete(DeleteBehavior.Restrict);
            charge.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.CancelledByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(subscription => subscription.Charges).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsOne(subscription => subscription.ScheduledChange, change =>
        {
            change.Property(item => item.TargetPlanId).HasColumnName("ScheduledPlanId");
            change.Property(item => item.TargetVersionId).HasColumnName("ScheduledVersionId");
            change.Property(item => item.TargetTerm).HasColumnName("ScheduledTerm");
            change.Property(item => item.TargetLicences).HasColumnName("ScheduledLicences");
            change.Property(item => item.EffectiveUtc).HasColumnName("ScheduledEffectiveUtc");
            change.Property(item => item.RequestedByUserId).HasColumnName("ScheduledByUserId");
            change.Property(item => item.RequestedUtc).HasColumnName("ScheduledUtc");
        });
    }
}
