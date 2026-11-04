using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsRestrictedItemConfiguration.cs
//
// PURPOSE:
// Maps OperationsRestrictedItem to the database (Slice 3C).
//
// RESPONSIBILITIES:
// - Columns, indexes and links
//
// IMPORTANT NOTES:
// - One entry per name in a company: a double click never lists an item twice (ADR-0019)
// =========================================================
internal sealed class OperationsRestrictedItemConfiguration : IEntityTypeConfiguration<OperationsRestrictedItem>
{
    public void Configure(EntityTypeBuilder<OperationsRestrictedItem> builder)
    {
        builder.ToTable("RestrictedItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(OperationsInspectionLimits.NameMaxLength).IsRequired();
        builder.Property(item => item.Words).HasMaxLength(OperationsInspectionLimits.WordsMaxLength).IsRequired();
        builder.Property(item => item.Countries).HasMaxLength(OperationsInspectionLimits.CountriesMaxLength).IsUnicode(false);
        builder.Property(item => item.Carriers).HasMaxLength(OperationsInspectionLimits.CarriersMaxLength);
        builder.Property(item => item.Requirement).HasMaxLength(OperationsInspectionLimits.RequirementMaxLength);
        builder.HasIndex(item => new { item.TenantId, item.Name }).IsUnique();
    }
}
