using System.Text.RegularExpressions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformRole.cs
//
// PURPOSE:
// A named set of Platform permissions that can be granted to
// staff users.
//
// RESPONSIBILITIES:
// - Hold the name, active state and permission codes
// - Protect system roles: they cannot be renamed, deactivated
//   or edited from the console
//
// IMPORTANT NOTES:
// - Aggregate root
// - A system role has a SystemKey (for example
//   security_administrator). Its display name comes from
//   resources in en, fr and es; its permissions are set from
//   code at deploy through SynchronizeSystemPermissions
// - A custom role's name is the text its creator typed and is
//   not translated (operational data, L-5)
// - Platform-wide only. No department scope (decision D-S3)
// - V3 exposed public setters on PlatformRole; V4 changes it
//   only through these methods
// =========================================================
public sealed class PlatformRole : AuditableEntity
{
    private static readonly Regex PermissionCodePattern = new(
        @"^platform(\.[a-z][a-z0-9_]*)+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SystemKeyPattern = new(
        @"^[a-z][a-z0-9_]*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly List<PlatformRolePermission> _permissions = new();

    private PlatformRole()
    {
        Name = string.Empty;
    }

    private PlatformRole(string name, string? systemKey, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        Name = name;
        SystemKey = systemKey;
        IsActive = true;
    }

    public string Name { get; private set; }

    public string? SystemKey { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsSystem => SystemKey is not null;

    public IReadOnlyList<PlatformRolePermission> Permissions => _permissions;

    // =====================================================
    // METHOD: CREATE
    // =====================================================
    public static PlatformRole CreateCustom(string name, DateTimeOffset createdUtc)
    {
        return new PlatformRole(PlatformIdentityText.RequiredName(name), null, createdUtc);
    }

    public static PlatformRole CreateSystem(string systemKey, DateTimeOffset createdUtc)
    {
        if (systemKey is null
            || systemKey.Length > PlatformIdentityLimits.SystemRoleKeyMaxLength
            || !SystemKeyPattern.IsMatch(systemKey))
        {
            throw new DomainException(PlatformIdentityErrors.SystemRoleKeyInvalid);
        }

        return new PlatformRole(systemKey, systemKey, createdUtc);
    }

    // =====================================================
    // METHOD: CHANGE
    // =====================================================
    public void Rename(string name, DateTimeOffset utcNow)
    {
        EnsureCustom();

        Name = PlatformIdentityText.RequiredName(name);
        MarkAsModified(utcNow);
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        EnsureCustom();

        if (!IsActive)
        {
            throw new DomainException(PlatformIdentityErrors.RoleAlreadyInactive);
        }

        IsActive = false;
        MarkAsModified(utcNow);
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        if (IsActive)
        {
            throw new DomainException(PlatformIdentityErrors.RoleAlreadyActive);
        }

        IsActive = true;
        MarkAsModified(utcNow);
    }

    public void ReplacePermissions(IEnumerable<string> permissionCodes, DateTimeOffset utcNow)
    {
        EnsureCustom();
        ApplyPermissions(permissionCodes, utcNow);
    }

    public void SynchronizeSystemPermissions(IEnumerable<string> permissionCodes, DateTimeOffset utcNow)
    {
        if (!IsSystem)
        {
            throw new DomainException(PlatformIdentityErrors.SystemRoleKeyInvalid);
        }

        ApplyPermissions(permissionCodes, utcNow);
    }

    public bool Grants(string permissionCode)
    {
        return IsActive && _permissions.Any(permission => permission.PermissionCode == permissionCode);
    }

    private void ApplyPermissions(IEnumerable<string> permissionCodes, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(permissionCodes);

        HashSet<string> wanted = new(StringComparer.Ordinal);

        foreach (string code in permissionCodes)
        {
            if (code is null
                || code.Length > PlatformIdentityLimits.PermissionCodeMaxLength
                || !PermissionCodePattern.IsMatch(code))
            {
                throw new DomainException(PlatformIdentityErrors.PermissionCodeInvalid);
            }

            wanted.Add(code);
        }

        _permissions.RemoveAll(permission => !wanted.Contains(permission.PermissionCode));

        foreach (string code in wanted.Where(code => _permissions.All(permission => permission.PermissionCode != code)))
        {
            _permissions.Add(new PlatformRolePermission(Id, code, utcNow));
        }

        MarkAsModified(utcNow);
    }

    private void EnsureCustom()
    {
        if (IsSystem)
        {
            throw new DomainException(PlatformIdentityErrors.SystemRoleProtected);
        }
    }
}
