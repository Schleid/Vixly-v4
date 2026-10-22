using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsRoleConfiguration.cs
//
// PURPOSE:
// Maps roles to ops.Roles (Slice 1D).
//
// RESPONSIBILITIES:
// - Store the name, the Administrator flag, the active flag and the permission codes
//
// IMPORTANT NOTES:
// - One Administrator role per tenant; tenant role names unique per tenant
// - Permissions are one column of catalogue codes
// =========================================================
internal sealed class OperationsRoleConfiguration : IEntityTypeConfiguration<OperationsRole>
{
    public void Configure(EntityTypeBuilder<OperationsRole> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(role => role.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(role => role.Name).HasMaxLength(OperationsAccessLimits.RoleNameMaxLength).IsRequired();
        builder.Ignore(role => role.Permissions);
        builder.Property<string>("PermissionsText").HasColumnName("Permissions").HasMaxLength(2000).IsUnicode(false).IsRequired();
        builder.HasIndex(role => new { role.TenantId, role.Name }).IsUnique();
        builder.HasIndex(role => role.TenantId).IsUnique().HasFilter("[IsAdministrator] = 1").HasDatabaseName("IX_Roles_TenantId_Administrator");
    }
}
