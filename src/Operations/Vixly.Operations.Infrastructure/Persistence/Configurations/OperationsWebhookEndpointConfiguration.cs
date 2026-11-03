using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Api;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsWebhookEndpointConfiguration.cs
//
// PURPOSE:
// Maps the company’s webhook addresses (Slice 10).
//
// RESPONSIBILITIES:
// - Columns and indexes; one place (event version) per kind
//
// IMPORTANT NOTES:
// - The signing secret only protected
// =========================================================
internal sealed class OperationsWebhookEndpointConfiguration : IEntityTypeConfiguration<OperationsWebhookEndpoint>
{
    public void Configure(EntityTypeBuilder<OperationsWebhookEndpoint> builder)
    {
        builder.ToTable("WebhookEndpoints");
        builder.HasKey(endpoint => endpoint.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(endpoint => endpoint.Url).HasMaxLength(OperationsWebhookEndpoint.UrlMaxLength).IsRequired();
        builder.Property(endpoint => endpoint.SecretProtected).HasMaxLength(2000).IsUnicode(false).IsRequired();
        builder.Property(endpoint => endpoint.SecretHint).HasMaxLength(4).IsUnicode(false).IsRequired();
        builder.HasIndex(endpoint => new { endpoint.TenantId, endpoint.Active });
    }
}
