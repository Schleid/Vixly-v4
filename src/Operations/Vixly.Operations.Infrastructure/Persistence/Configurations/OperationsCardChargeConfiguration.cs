using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCardChargeConfiguration.cs
//
// PURPOSE:
// Maps a card charge asked of Stripe for an invoice to the database (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - One started charge per invoice (filtered unique index)
// =========================================================
internal sealed class OperationsCardChargeConfiguration : IEntityTypeConfiguration<OperationsCardCharge>
{
    public void Configure(EntityTypeBuilder<OperationsCardCharge> builder)
    {
        builder.ToTable("CardCharges");
        builder.HasKey(charge => charge.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(charge => charge.Amount).HasPrecision(14, 2);
        builder.Property(charge => charge.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsRequired();
        builder.Property(charge => charge.CheckoutSessionId).HasMaxLength(255).IsUnicode(false);
        builder.Property(charge => charge.PaymentIntentId).HasMaxLength(255).IsUnicode(false);
        builder.Property(charge => charge.PageUrl).HasMaxLength(2000).IsUnicode(false);
        // The lines before the card (owner, 2026-10-26), as JSON; empty for a card that pays everything.
        builder.Property(charge => charge.OtherTenders).HasColumnName("OtherTenders")
            .HasConversion(
                tenders => tenders.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(tenders, (System.Text.Json.JsonSerializerOptions?)null),
                json => json == null ? new List<OperationsTenderDraft>() : System.Text.Json.JsonSerializer.Deserialize<List<OperationsTenderDraft>>(json, (System.Text.Json.JsonSerializerOptions?)null)!,
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<OperationsTenderDraft>>(
                    (left, right) => left!.SequenceEqual(right!),
                    tenders => tenders.Aggregate(0, (hash, tender) => HashCode.Combine(hash, tender.GetHashCode())),
                    tenders => tenders.ToList()))
            .IsRequired(false);
        builder.HasOne<OperationsInvoice>().WithMany().HasForeignKey(charge => charge.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(charge => charge.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(charge => charge.InvoiceId).IsUnique().HasFilter("[Status] = 1").HasDatabaseName("IX_CardCharges_InvoiceId_Started");
        builder.HasIndex(charge => new { charge.TenantId, charge.InvoiceId });
    }
}
