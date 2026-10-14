using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Subscriptions;

// =========================================================
// FILE: PlatformPlanVersionConfiguration.cs
//
// PURPOSE:
// Maps plan versions with their limits and features.
//
// RESPONSIBILITIES:
// - Map PlanVersions, PlanVersionLimits and PlanVersionFeatures
//
// IMPORTANT NOTES:
// - Money is decimal(19,4)
// - At most one draft per plan, submitted or not (filtered unique index on Draft and PendingApproval)
// - "One published version" is kept by the plan aggregate and the
//   Plans row version, not by an index: publishing updates two
//   version rows, and SQL Server checks a unique index after each
//   statement, so the order EF sends them in could break it
// =========================================================
internal sealed class PlatformPlanVersionConfiguration : IEntityTypeConfiguration<PlatformPlanVersion>
{
    public void Configure(EntityTypeBuilder<PlatformPlanVersion> builder)
    {
        builder.ToTable("PlanVersions");
        builder.HasKey(version => version.Id);
        builder.Property(version => version.CurrencyCode)
            .HasMaxLength(PlatformSubscriptionLimits.CurrencyCodeLength)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();
        builder.Property(version => version.MonthlyPricePerLicence).HasPrecision(19, 4);
        builder.Property(version => version.AnnualPricePerLicence).HasPrecision(19, 4);
        builder.Property(version => version.SetupFee).HasPrecision(19, 4);
        builder.HasIndex(version => new { version.PlatformPlanId, version.Number }).IsUnique();
        builder.HasIndex(version => version.PlatformPlanId, "IX_PlanVersions_OneDraft")
            .IsUnique()
            .HasFilter("[Status] IN (0, 3)");
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(version => version.PublishedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(version => version.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(version => version.RejectedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(version => version.RejectionReason).HasMaxLength(PlatformSubscriptionLimits.ReasonMaxLength);
        builder.Ignore(version => version.IsAwaitingApproval);

        builder.Ignore(version => version.IsDraft);
        builder.Ignore(version => version.OffersTrial);
        builder.Ignore(version => version.Terms);

        builder.OwnsMany(version => version.Limits, limit =>
        {
            limit.ToTable("PlanVersionLimits");
            limit.WithOwner().HasForeignKey("PlanVersionId");
            limit.HasKey("PlanVersionId", nameof(PlatformPlanVersionLimit.Kind));
        });
        builder.Navigation(version => version.Limits).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(version => version.Features, feature =>
        {
            feature.ToTable("PlanVersionFeatures");
            feature.WithOwner().HasForeignKey("PlanVersionId");
            feature.HasKey("PlanVersionId", nameof(PlatformPlanVersionFeature.Feature));
        });
        builder.Navigation(version => version.Features).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
