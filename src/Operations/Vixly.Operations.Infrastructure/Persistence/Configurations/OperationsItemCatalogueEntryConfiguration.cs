using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Items;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsItemCatalogueEntryConfiguration.cs
//
// PURPOSE:
// Maps the company's item catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Table ItemCatalogue
//
// IMPORTANT NOTES:
// - Tenant filter and audit columns come from the model conventions
// =========================================================
internal sealed class OperationsItemCatalogueEntryConfiguration : IEntityTypeConfiguration<OperationsItemCatalogueEntry>
{
    public void Configure(EntityTypeBuilder<OperationsItemCatalogueEntry> builder)
    {
        builder.ToTable("ItemCatalogue");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Description).HasMaxLength(OperationsParcelItem.DescriptionMaxLength).IsRequired();
        builder.Property(item => item.Matches).HasMaxLength(OperationsItemCatalogueEntry.MatchesMaxLength);
        builder.Property(item => item.HsCode).HasMaxLength(OperationsHsCode.MaxDigits).IsUnicode(false).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.Description });
    }
}
