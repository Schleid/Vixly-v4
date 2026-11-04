using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Insurance;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsLiabilityTermsConfiguration.cs
//
// PURPOSE:
// Maps the company's liability terms to ops.LiabilityTerms (Slice 5B).
//
// RESPONSIBILITIES:
// - One row per company and transport mode
//
// IMPORTANT NOTES:
// - The amount per weight keeps the unit it was entered in
// =========================================================
internal sealed class OperationsLiabilityTermsConfiguration : IEntityTypeConfiguration<OperationsLiabilityTerms>
{
    public void Configure(EntityTypeBuilder<OperationsLiabilityTerms> builder)
    {
        builder.ToTable("LiabilityTerms");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Amount).HasPrecision(14, 4);
        builder.Property(item => item.Cap).HasPrecision(14, 2);
        builder.Property(item => item.CustomerText).HasMaxLength(OperationsLiabilityTerms.TextMaxLength);
        builder.HasIndex(item => new { item.TenantId, item.Mode }).IsUnique();
    }
}
