using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsRoleGrantConfiguration.cs
//
// PURPOSE:
// Maps role grants to ops.RoleGrants (Slice 1D).
//
// RESPONSIBILITIES:
// - Store user, role, scope, who granted and revoked, and when
//
// IMPORTANT NOTES:
// - Grants are never deleted; revoked ones stay as history
// =========================================================
internal sealed class OperationsRoleGrantConfiguration : IEntityTypeConfiguration<OperationsRoleGrant>
{
    public void Configure(EntityTypeBuilder<OperationsRoleGrant> builder)
    {
        builder.ToTable("RoleGrants");
        builder.HasKey(grant => grant.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(grant => grant.IsActive);
        builder.HasIndex(grant => new { grant.TenantId, grant.UserId });
        builder.HasIndex(grant => new { grant.TenantId, grant.RoleId });
    }
}
