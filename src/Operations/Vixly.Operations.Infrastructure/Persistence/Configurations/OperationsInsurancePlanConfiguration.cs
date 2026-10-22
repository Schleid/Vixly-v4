using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsInsurancePlanConfiguration.cs
//
// PURPOSE:
// Maps OperationsInsurancePlan to the database (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - The JSON versions with their bands, and the one-per-company index
//
// IMPORTANT NOTES:
// - One plan per company (A-6)
// =========================================================
internal sealed class OperationsInsurancePlanConfiguration : IEntityTypeConfiguration<OperationsInsurancePlan>
{
    public void Configure(EntityTypeBuilder<OperationsInsurancePlan> builder)
    {
        builder.ToTable("InsurancePlans");
        builder.HasKey(plan => plan.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.OwnsMany(plan => plan.Versions, version =>
        {
            version.ToJson("Versions");
            version.Property(item => item.Percent).HasPrecision(6, 3);
            version.Property(item => item.MinimumPremium).HasPrecision(12, 2);
            version.Property(item => item.MaxValue).HasPrecision(12, 2);
            version.OwnsMany(item => item.Bands, band =>
            {
                band.Property(value => value.UpToValue).HasPrecision(12, 2);
                band.Property(value => value.Premium).HasPrecision(12, 2);
            });
            version.Navigation(item => item.Bands).UsePropertyAccessMode(PropertyAccessMode.Field);

            // Slice 4C: who covers what, the exclusions, the payout and the claim rules.
            version.OwnsOne(item => item.Coverage, coverage =>
            {
                coverage.Property(value => value.LiabilityPerUnit).HasPrecision(14, 4);
                coverage.Property(value => value.LiabilityPerShipment).HasPrecision(12, 2);
                coverage.Property(value => value.DeductibleAmount).HasPrecision(12, 2);
                coverage.Property(value => value.DeductiblePercent).HasPrecision(6, 3);
                coverage.Property(value => value.MaxPerItem).HasPrecision(12, 2);
            });
            version.Navigation(item => item.Coverage).IsRequired();
        });
        builder.Navigation(plan => plan.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property(plan => plan.Mode).HasDefaultValue(OperationsTransportMode.Air).HasSentinel((OperationsTransportMode)0);

        // Slice 5B: the company policy the cover sits under; none: self-insured.
        builder.HasOne<Vixly.Operations.Domain.Insurance.OperationsInsurancePolicy>().WithMany().HasForeignKey(plan => plan.PolicyId).OnDelete(DeleteBehavior.Restrict);

        // Slice 5D: several products per mode, named, described and ordered.
        builder.Property(plan => plan.Name).HasMaxLength(OperationsInsurancePlan.NameMaxLength).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(plan => plan.PitchEn).HasMaxLength(OperationsInsurancePlan.PitchMaxLength);
        builder.Property(plan => plan.PitchFr).HasMaxLength(OperationsInsurancePlan.PitchMaxLength);
        builder.Property(plan => plan.PitchEs).HasMaxLength(OperationsInsurancePlan.PitchMaxLength);
        builder.HasIndex(plan => new { plan.TenantId, plan.Mode, plan.Position });
    }
}
