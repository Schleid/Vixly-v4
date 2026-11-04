using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsInspectionRuleConfiguration.cs
//
// PURPOSE:
// Maps OperationsInspectionRule to the database (Slice 3C).
//
// RESPONSIBILITIES:
// - Columns, indexes and links
//
// IMPORTANT NOTES:
// - The value is text: an amount, a carrier or a country code
// =========================================================
internal sealed class OperationsInspectionRuleConfiguration : IEntityTypeConfiguration<OperationsInspectionRule>
{
    public void Configure(EntityTypeBuilder<OperationsInspectionRule> builder)
    {
        builder.ToTable("InspectionRules");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Value).HasMaxLength(OperationsInspectionLimits.RuleValueMaxLength);
        builder.Ignore(item => item.Amount);
        // One rule of each kind and value, the list rule included (its value is empty): a double click never adds it twice (ADR-0019).
        builder.HasIndex(item => new { item.TenantId, item.Kind, item.Value }).IsUnique().HasFilter(null);
    }
}
