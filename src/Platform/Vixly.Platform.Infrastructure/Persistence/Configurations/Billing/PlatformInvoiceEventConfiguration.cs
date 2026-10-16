using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Infrastructure.Persistence.Conversions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Billing;

// =========================================================
// FILE: PlatformInvoiceEventConfiguration.cs
//
// PURPOSE:
// Maps invoice history.
//
// RESPONSIBILITIES:
// - Map the entity to its table, columns and indexes
//
// IMPORTANT NOTES:
// - Append-only (DENY UPDATE, DELETE in the global script)
// =========================================================
internal sealed class PlatformInvoiceEventConfiguration : IEntityTypeConfiguration<PlatformInvoiceEvent>
{
    public void Configure(EntityTypeBuilder<PlatformInvoiceEvent> builder)
    {
        builder.ToTable("InvoiceEvents");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.EventCode).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(PlatformBillingLimits.ReasonMaxLength);
        builder.Property(item => item.Arguments).HasConversion(PlatformValueConverters.Arguments, PlatformValueConverters.ArgumentsComparer).IsRequired();
        builder.HasOne<PlatformInvoice>().WithMany().HasForeignKey(item => item.PlatformInvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.PlatformInvoiceId, item.CreatedUtc });
    }
}
