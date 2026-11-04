using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCarrierConfiguration.cs
//
// PURPOSE:
// Maps the company's carriers to ops.Carriers (Slice 6B).
//
// RESPONSIBILITIES:
// - Name, kind, account number, airline prefix
//
// IMPORTANT NOTES:
// - A name is unique within the company
// =========================================================
internal sealed class OperationsCarrierConfiguration : IEntityTypeConfiguration<OperationsCarrier>
{
    public void Configure(EntityTypeBuilder<OperationsCarrier> builder)
    {
        builder.ToTable("Carriers");
        builder.HasKey(carrier => carrier.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(carrier => carrier.Name).HasMaxLength(OperationsCarrier.NameMaxLength).IsRequired();
        builder.Property(carrier => carrier.AccountNumber).HasMaxLength(OperationsCarrier.AccountMaxLength);
        builder.Property(carrier => carrier.AirlinePrefix).HasMaxLength(3).IsFixedLength().IsUnicode(false);
        builder.HasIndex(carrier => new { carrier.TenantId, carrier.Name }).IsUnique();
    }
}
