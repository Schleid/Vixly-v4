using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsBranchConfiguration.cs
//
// PURPOSE:
// Maps branches to ops.Branches (Slice 1B).
//
// RESPONSIBILITIES:
// - Code unique per tenant
// - Address columns prefixed Address_
//
// IMPORTANT NOTES:
// - Row version guards concurrent edits
// =========================================================
internal sealed class OperationsBranchConfiguration : IEntityTypeConfiguration<OperationsBranch>
{
    public void Configure(EntityTypeBuilder<OperationsBranch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Code).HasMaxLength(OperationsLocationLimits.CodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(OperationsLocationLimits.NameMaxLength).IsRequired();
        builder.Property(item => item.Phone).HasMaxLength(OperationsLocationLimits.PhoneMaxLength).IsUnicode(false);
        builder.Property(item => item.Email).HasMaxLength(OperationsLocationLimits.EmailMaxLength);
        builder.Property(item => item.OpeningHours).HasMaxLength(OperationsLocationLimits.HoursMaxLength);
        builder.Ignore(item => item.IsPublic);
        builder.HasIndex(item => new { item.TenantId, item.Code }).IsUnique();
        builder.OwnsOne(item => item.Address, OperationsAddressMapping.Map);
        builder.Navigation(item => item.Address).IsRequired();

    }
}
