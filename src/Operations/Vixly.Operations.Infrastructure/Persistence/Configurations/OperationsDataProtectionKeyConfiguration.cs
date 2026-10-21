using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsDataProtectionKeyConfiguration.cs
//
// PURPOSE:
// Table ops.DataProtectionKeys (S-5).
//
// RESPONSIBILITIES:
// - Map the key ring table
//
// IMPORTANT NOTES:
// - Shared by Operations.Web and Operations.Api, each under its own application name
// =========================================================
internal sealed class OperationsDataProtectionKeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
{
    public void Configure(EntityTypeBuilder<DataProtectionKey> builder)
    {
        builder.ToTable("DataProtectionKeys");
    }
}
