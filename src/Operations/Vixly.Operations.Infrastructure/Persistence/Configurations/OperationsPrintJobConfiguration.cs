using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Printing;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsPrintJobConfiguration.cs
//
// PURPOSE:
// Maps waiting labels to ops.PrintJobs (Slice 3B).
//
// RESPONSIBILITIES:
// - Columns, the waiting-jobs index and the printer link
//
// IMPORTANT NOTES:
// - A row version: one print program takes a job at a time
// =========================================================
internal sealed class OperationsPrintJobConfiguration : IEntityTypeConfiguration<OperationsPrintJob>
{
    public void Configure(EntityTypeBuilder<OperationsPrintJob> builder)
    {
        builder.ToTable("PrintJobs");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Payload).HasMaxLength(4000).IsUnicode(false).IsRequired();
        builder.Property(item => item.ErrorCode).HasMaxLength(60).IsUnicode(false);
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId, item.Status, item.CreatedUtc });
        builder.HasOne<OperationsLabelPrinter>().WithMany().HasForeignKey(item => item.PrinterId).OnDelete(DeleteBehavior.Restrict);
    }
}
