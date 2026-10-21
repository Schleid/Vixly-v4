using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsItemTypeConfiguration.cs
//
// PURPOSE:
// Maps OperationsItemType to the database (Slice 4B).
//
// RESPONSIBILITIES:
// - Columns and the one-name-per-company index
//
// IMPORTANT NOTES:
// - Never deleted, taken out of use
// =========================================================
internal sealed class OperationsItemTypeConfiguration : IEntityTypeConfiguration<OperationsItemType>
{
    public void Configure(EntityTypeBuilder<OperationsItemType> builder)
    {
        builder.ToTable("ItemTypes");
        builder.HasKey(type => type.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(type => type.Name).HasMaxLength(OperationsChargeLimits.NameMaxLength).IsRequired();
        builder.Property(type => type.Words).HasMaxLength(OperationsChargeLimits.WordsMaxLength);
        builder.HasIndex(type => new { type.TenantId, type.Name }).IsUnique();
    }
}
