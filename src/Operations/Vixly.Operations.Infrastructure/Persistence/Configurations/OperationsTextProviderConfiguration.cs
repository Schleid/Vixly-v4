using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Texts;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsTextProviderConfiguration.cs
//
// PURPOSE:
// Maps the company's Twilio account (Slice 9).
//
// RESPONSIBILITIES:
// - Columns and indexes; the WhatsApp templates as JSON
//
// IMPORTANT NOTES:
// - One per company; the token only protected
// =========================================================
internal sealed class OperationsTextProviderConfiguration : IEntityTypeConfiguration<OperationsTextProvider>
{
    public void Configure(EntityTypeBuilder<OperationsTextProvider> builder)
    {
        builder.ToTable("TextProviders");
        builder.HasKey(provider => provider.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(provider => provider.AccountSid).HasMaxLength(34).IsUnicode(false).IsRequired();
        builder.Property(provider => provider.AuthTokenProtected).HasMaxLength(2000).IsUnicode(false).IsRequired();
        builder.Property(provider => provider.TokenHint).HasMaxLength(4).IsUnicode(false).IsRequired();
        builder.Property(provider => provider.AccountName).HasMaxLength(OperationsTextProvider.NameMaxLength);
        builder.Property(provider => provider.SmsFrom).HasMaxLength(34).IsUnicode(false);
        builder.Property(provider => provider.WhatsAppFrom).HasMaxLength(16).IsUnicode(false);
        builder.Property(provider => provider.CallbackUrl).HasMaxLength(OperationsTextProvider.CallbackUrlMaxLength).IsUnicode(false);
        builder.Ignore(provider => provider.SendsSms);
        builder.Ignore(provider => provider.SendsWhatsApp);
        builder.OwnsMany(provider => provider.Templates, template =>
        {
            template.ToJson("Templates");
        });
        builder.Navigation(provider => provider.Templates).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(provider => provider.TenantId).IsUnique();
    }
}
