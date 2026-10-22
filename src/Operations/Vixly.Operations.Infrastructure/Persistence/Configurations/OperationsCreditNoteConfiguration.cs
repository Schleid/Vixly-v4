using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCreditNoteConfiguration.cs
//
// PURPOSE:
// Maps OperationsCreditNote to the database (Slice 5F step 1).
//
// RESPONSIBILITIES:
// - Columns, the JSON lines and taxes, and indexes
//
// IMPORTANT NOTES:
// - Number unique per company once approved; one request waiting per invoice; a form key used once
// =========================================================
internal sealed class OperationsCreditNoteConfiguration : IEntityTypeConfiguration<OperationsCreditNote>
{
    public void Configure(EntityTypeBuilder<OperationsCreditNote> builder)
    {
        builder.ToTable("CreditNotes");
        builder.HasKey(note => note.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(note => note.Number).HasMaxLength(20).IsUnicode(false);
        builder.Property(note => note.Reason).HasMaxLength(OperationsBillingLimits.ReasonMaxLength).IsRequired();
        builder.Property(note => note.RefusalNote).HasMaxLength(OperationsBillingLimits.ReasonMaxLength);
        builder.Property(note => note.Total).HasPrecision(14, 2);
        builder.Ignore(note => note.Subtotal);
        builder.Ignore(note => note.TaxTotal);
        builder.OwnsMany(note => note.Lines, line =>
        {
            line.ToJson("Lines");
            line.Property(item => item.Amount).HasPrecision(14, 2);
        });
        builder.OwnsMany(note => note.Taxes, tax =>
        {
            tax.ToJson("Taxes");
            tax.Property(item => item.Amount).HasPrecision(14, 2);
        });
        builder.Navigation(note => note.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(note => note.Taxes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<OperationsInvoice>().WithMany().HasForeignKey(note => note.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(note => new { note.TenantId, note.Number }).IsUnique().HasFilter("[Number] IS NOT NULL");
        builder.HasIndex(note => new { note.TenantId, note.InvoiceId }).IsUnique().HasFilter("[Status] = 1");
        builder.HasIndex(note => new { note.TenantId, note.InvoiceId, note.Status });
        builder.HasIndex(note => new { note.TenantId, note.RequestKey }).IsUnique();
        builder.HasIndex(note => new { note.TenantId, note.WarehouseId, note.Status });
    }
}
