using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformRolePermission.cs
//
// PURPOSE:
// One permission code granted by a role.
//
// RESPONSIBILITIES:
// - Hold the permission code
//
// IMPORTANT NOTES:
// - Part of the PlatformRole aggregate
// - The code is the permission's identity. The list of valid
//   codes lives in code (the permission catalogue) and is
//   checked by the use case before it reaches the role
// =========================================================
public sealed class PlatformRolePermission : AuditableEntity
{
    private PlatformRolePermission()
    {
        PermissionCode = string.Empty;
    }

    internal PlatformRolePermission(Guid platformRoleId, string permissionCode, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformRoleId = platformRoleId;
        PermissionCode = permissionCode;
    }

    public Guid PlatformRoleId { get; private set; }

    public string PermissionCode { get; private set; }
}
