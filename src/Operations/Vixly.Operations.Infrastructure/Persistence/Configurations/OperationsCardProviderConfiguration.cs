using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCardProviderConfiguration.cs
//
// PURPOSE:
// Maps the company's Stripe account to the database (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - One per company; the keys stored protected with the server's key, never in clear
// =========================================================
internal sealed class OperationsCardProviderConfiguration : IEntityTypeConfiguration<OperationsCardProvider>
{
    public void Configure(EntityTypeBuilder<OperationsCardProvider> builder)
    {
        builder.ToTable("CardProviders");
        builder.HasKey(provider => provider.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(provider => provider.SecretKeyProtected).HasMaxLength(2000).IsUnicode(false).IsRequired();
        builder.Property(provider => provider.WebhookSecretProtected).HasMaxLength(2000).IsUnicode(false).IsRequired();
        builder.Property(provider => provider.PublishableKey).HasMaxLength(255).IsUnicode(false).IsRequired();
        builder.Property(provider => provider.KeyHint).HasMaxLength(8).IsUnicode(false).IsRequired();
        builder.Property(provider => provider.AccountName).HasMaxLength(200).IsRequired();
        builder.HasIndex(provider => provider.TenantId).IsUnique();
    }
}
