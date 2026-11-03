using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Api;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsApiEventConfiguration.cs
//
// PURPOSE:
// Maps the events kept for the company’s webhooks (Slice 10; review).
//
// RESPONSIBILITIES:
// - Columns and indexes; the version the database gives each row as it is saved
//
// IMPORTANT NOTES:
// - Read by kind after a version, up to MIN_ACTIVE_ROWVERSION()
// =========================================================
internal sealed class OperationsApiEventConfiguration : IEntityTypeConfiguration<OperationsApiEvent>
{
    public void Configure(EntityTypeBuilder<OperationsApiEvent> builder)
    {
        builder.ToTable("ApiEvents");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("Version").IsRowVersion();
        builder.HasIndex("TenantId", "Kind", "Version");
        builder.HasIndex(item => item.CreatedUtc);
    }
}
