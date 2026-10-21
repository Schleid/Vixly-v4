using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Insurance;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsInsuranceCertificateConfiguration.cs
//
// PURPOSE:
// Maps the insurance certificates of shipments to ops.InsuranceCertificates (Slice 5B).
//
// RESPONSIBILITIES:
// - Unique numbers; one live certificate per shipment
//
// IMPORTANT NOTES:
// - Linked to the shipment, its cover and policy without cascade
// =========================================================
internal sealed class OperationsInsuranceCertificateConfiguration : IEntityTypeConfiguration<OperationsInsuranceCertificate>
{
    public void Configure(EntityTypeBuilder<OperationsInsuranceCertificate> builder)
    {
        builder.ToTable("InsuranceCertificates");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(item => item.IsLive);
        builder.Property(item => item.Number).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(item => item.DeclaredValue).HasPrecision(14, 2);
        builder.Property(item => item.Premium).HasPrecision(14, 2);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsUnicode(false);
        builder.HasIndex(item => new { item.TenantId, item.Number }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.ShipmentId }).IsUnique().HasFilter("[Status] <> 3");
        builder.HasIndex(item => new { item.TenantId, item.PolicyId, item.CreatedUtc });
        builder.HasOne<Vixly.Operations.Domain.Shipments.OperationsShipment>().WithMany().HasForeignKey(item => item.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vixly.Operations.Domain.Charges.OperationsInsurancePlan>().WithMany().HasForeignKey(item => item.PlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsInsurancePolicy>().WithMany().HasForeignKey(item => item.PolicyId).OnDelete(DeleteBehavior.Restrict);
    }
}
