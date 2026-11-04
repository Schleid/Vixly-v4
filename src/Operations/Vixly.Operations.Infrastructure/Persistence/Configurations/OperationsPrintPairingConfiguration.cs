using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsPrintPairingConfiguration.cs
//
// PURPOSE:
// Maps the one-time codes of Vixly Print phones (Slice 12E) to ops.PrintPairings.
//
// RESPONSIBILITIES:
// - Codes unique; a row version so a code lets one phone in
//
// IMPORTANT NOTES:
// - Only the code's hash is stored
// =========================================================
internal sealed class OperationsPrintPairingConfiguration : IEntityTypeConfiguration<OperationsPrintPairing>
{
    public void Configure(EntityTypeBuilder<OperationsPrintPairing> builder)
    {
        builder.ToTable("PrintPairings");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.CodeHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(OperationsPrintAgent.NameMaxLength).IsRequired();
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasIndex(item => item.CodeHash).IsUnique();
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(item => item.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
