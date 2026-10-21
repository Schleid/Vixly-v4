using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsPrintAgentConfiguration.cs
//
// PURPOSE:
// Maps dock print programs to ops.PrintAgents (Slice 3B).
//
// RESPONSIBILITIES:
// - Columns and the warehouse link
//
// IMPORTANT NOTES:
// - Only the key hash is stored
// =========================================================
internal sealed class OperationsPrintAgentConfiguration : IEntityTypeConfiguration<OperationsPrintAgent>
{
    public void Configure(EntityTypeBuilder<OperationsPrintAgent> builder)
    {
        builder.ToTable("PrintAgents");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(OperationsPrintAgent.NameMaxLength).IsRequired();
        builder.Property(item => item.KeyHash).HasMaxLength(128).IsUnicode(false).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId });
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(item => item.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
