using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsShippingServiceSetupConfiguration.cs
//
// PURPOSE:
// Maps the shipping-service setups to ops.ShippingServiceSetups (Slice 1E).
//
// RESPONSIBILITIES:
// - One row per tenant and service code
//
// IMPORTANT NOTES:
// - Divisor in cm³ per kg, three decimals
// =========================================================
internal sealed class OperationsShippingServiceSetupConfiguration : IEntityTypeConfiguration<OperationsShippingServiceSetup>
{
    public void Configure(EntityTypeBuilder<OperationsShippingServiceSetup> builder)
    {
        builder.ToTable("ShippingServiceSetups");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.ServiceCode).HasMaxLength(OperationsShippingServiceSetup.CodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Label).HasMaxLength(OperationsShippingServiceSetup.LabelMaxLength);
        builder.Property(item => item.VolumetricDivisor).HasPrecision(10, 3);
        builder.HasIndex(item => new { item.TenantId, item.ServiceCode }).IsUnique();
    }
}
