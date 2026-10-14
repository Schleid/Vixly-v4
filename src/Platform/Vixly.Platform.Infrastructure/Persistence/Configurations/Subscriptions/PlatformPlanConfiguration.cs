using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Infrastructure.Persistence.Conversions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Subscriptions;

// =========================================================
// FILE: PlatformPlanConfiguration.cs
//
// PURPOSE:
// Maps plans and their translations.
//
// RESPONSIBILITIES:
// - Map Plans and PlanTranslations
// - Link the plan to its versions
//
// IMPORTANT NOTES:
// - The code is unique; a row version guards concurrent edits and publication
// =========================================================
internal sealed class PlatformPlanConfiguration : IEntityTypeConfiguration<PlatformPlan>
{
    public void Configure(EntityTypeBuilder<PlatformPlan> builder)
    {
        builder.ToTable("Plans");
        builder.HasKey(plan => plan.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(plan => plan.Code)
            .HasMaxLength(PlatformSubscriptionLimits.PlanCodeMaxLength)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(plan => plan.Code).IsUnique();
        builder.HasIndex(plan => new { plan.Status, plan.IsPublic, plan.DisplayOrder });
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(plan => plan.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(plan => plan.Current);
        builder.Ignore(plan => plan.Draft);
        builder.Ignore(plan => plan.CanBeChosen);
        builder.Ignore(plan => plan.CurrencyCode);

        builder.OwnsMany(plan => plan.Translations, translation =>
        {
            translation.ToTable("PlanTranslations");
            translation.WithOwner().HasForeignKey(item => item.PlatformPlanId);
            translation.Property(item => item.Language)
                .HasConversion(PlatformValueConverters.Language)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsRequired();
            translation.HasKey(item => new { item.PlatformPlanId, item.Language });
            translation.Property(item => item.Name).HasMaxLength(PlatformSubscriptionLimits.PlanNameMaxLength).IsRequired();
            translation.Property(item => item.Description).HasMaxLength(PlatformSubscriptionLimits.PlanDescriptionMaxLength);
        });
        builder.Navigation(plan => plan.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(plan => plan.Versions)
            .WithOne()
            .HasForeignKey(version => version.PlatformPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(plan => plan.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
