using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Access;

// =========================================================
// FILE: OperationsRole.cs
//
// PURPOSE:
// A role: a named set of permissions (Slice 1D).
//
// RESPONSIBILITIES:
// - Create the Administrator system role and tenant roles
// - Rename, change permissions, deactivate and reactivate
//
// IMPORTANT NOTES:
// - The Owner is not a role: it is the user flag IsOwner (exactly one per tenant)
// - The Administrator role is fixed: its name comes from resources and its permissions from the catalogue
// - Tenant roles never hold system-only permissions
// =========================================================
public sealed class OperationsRole : OperationsEntity
{
    private const char Separator = ';';

    private OperationsRole()
    {
        Name = string.Empty;
        PermissionsText = string.Empty;
    }

    private OperationsRole(Guid tenantId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        Name = string.Empty;
        PermissionsText = string.Empty;
    }

    public string Name { get; private set; }

    public bool IsAdministrator { get; private set; }

    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<string> Permissions => IsAdministrator
        ? OperationsPermissions.Administrator
        : PermissionsText.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

    private string PermissionsText { get; set; }

    public static OperationsRole CreateAdministrator(Guid tenantId, DateTimeOffset utcNow)
    {
        return new OperationsRole(tenantId, utcNow) { Name = "administrator", IsAdministrator = true };
    }

    public static OperationsRole Create(Guid tenantId, string? name, IEnumerable<string> permissions, DateTimeOffset utcNow)
    {
        OperationsRole role = new(tenantId, utcNow) { Name = CheckName(name) };
        role.PermissionsText = Check(permissions);
        return role;
    }

    public bool Holds(string permission) => IsActive && Permissions.Contains(permission, StringComparer.Ordinal);

    public void Change(string? name, IEnumerable<string> permissions, DateTimeOffset utcNow)
    {
        RefuseSystem();
        Name = CheckName(name);
        PermissionsText = Check(permissions);
        MarkAsModified(utcNow);
    }

    public void SetActive(bool active, DateTimeOffset utcNow)
    {
        RefuseSystem();
        IsActive = active;
        MarkAsModified(utcNow);
    }

    private void RefuseSystem()
    {
        if (IsAdministrator)
        {
            throw new DomainException(OperationsRoleErrors.SystemRoleLocked);
        }
    }

    private static string CheckName(string? name)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException(OperationsRoleErrors.NameRequired);
        }

        return trimmed.Length > OperationsAccessLimits.RoleNameMaxLength ? throw new DomainException(OperationsRoleErrors.NameTooLong) : trimmed;
    }

    private static string Check(IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        List<string> codes = permissions.Where(code => !string.IsNullOrWhiteSpace(code)).Select(code => code.Trim()).Distinct(StringComparer.Ordinal).ToList();

        if (codes.Any(code => !OperationsPermissions.IsKnown(code)))
        {
            throw new DomainException(OperationsRoleErrors.PermissionUnknown);
        }

        if (codes.Any(code => OperationsPermissions.SystemOnly.Contains(code, StringComparer.Ordinal)))
        {
            throw new DomainException(OperationsRoleErrors.PermissionReserved);
        }

        // Kept in catalogue order, so the stored text does not depend on how the form was filled in.
        return string.Join(Separator, OperationsPermissions.All.Where(code => codes.Contains(code, StringComparer.Ordinal)));
    }
}
