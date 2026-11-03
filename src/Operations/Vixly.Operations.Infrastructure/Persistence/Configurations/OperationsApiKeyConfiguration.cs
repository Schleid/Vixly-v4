using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Api;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsApiKeyConfiguration.cs
//
// PURPOSE:
// Maps the company API keys (Slice 10).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - Only the hash of the secret; the public id is unique everywhere
// =========================================================
internal sealed class OperationsApiKeyConfiguration : IEntityTypeConfiguration<OperationsApiKey>
{
    public void Configure(EntityTypeBuilder<OperationsApiKey> builder)
    {
        builder.ToTable("ApiKeys");
        builder.HasKey(key => key.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(key => key.Name).HasMaxLength(OperationsApiKey.NameMaxLength).IsRequired();
        builder.Property(key => key.KeyId).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(key => key.SecretHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(key => key.Hint).HasMaxLength(4).IsUnicode(false).IsRequired();
        builder.Ignore(key => key.Active);
        builder.HasIndex(key => key.KeyId).IsUnique();
        builder.HasIndex(key => key.TenantId);
    }
}
