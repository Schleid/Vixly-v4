using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsNumberSequenceConfiguration.cs
//
// PURPOSE:
// Maps the number sequences to ops.NumberSequences (D-O12).
//
// RESPONSIBILITIES:
// - One row per tenant, kind and year
//
// IMPORTANT NOTES:
// - Rows are written by OperationsNumberAllocator with one MERGE
// =========================================================
internal sealed class OperationsNumberSequenceConfiguration : IEntityTypeConfiguration<OperationsNumberSequence>
{
    public void Configure(EntityTypeBuilder<OperationsNumberSequence> builder)
    {
        builder.ToTable("NumberSequences");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.TenantId, item.Kind, item.Year }).IsUnique();
    }
}
