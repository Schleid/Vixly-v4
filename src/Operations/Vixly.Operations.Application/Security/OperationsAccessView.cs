using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.Security;

// =========================================================
// FILE: OperationsAccessView.cs
//
// PURPOSE:
// What a signed-in staff member may do, and where (Slice 1D).
//
// RESPONSIBILITIES:
// - Answer whether a permission is held anywhere, tenant-wide, or for a branch or warehouse
//
// IMPORTANT NOTES:
// - The Owner holds everything everywhere; the Administrator role is tenant-wide
// - A branch grant covers the branch and its warehouses
// - A partner agent (R-8) holds only the partner's access, at its warehouse, never company-wide
// =========================================================
public sealed class OperationsAccessView
{
    public OperationsAccessView(OperationsUser user, IReadOnlyList<(OperationsRoleGrant Grant, OperationsRole Role)> grants)
        : this(user, grants, null)
    {
    }

    // A partner agent (R-8) holds no role: only the partner's access at its warehouse.
    public OperationsAccessView(OperationsUser user, IReadOnlyList<(OperationsRoleGrant Grant, OperationsRole Role)> grants, OperationsPartnerScope? partner)
    {
        User = user ?? throw new ArgumentNullException(nameof(user));
        ArgumentNullException.ThrowIfNull(grants);
        Partner = user.IsPartnerAgent ? partner ?? new OperationsPartnerScope(user.PartnerWarehouseId!.Value, false, false) : null;
        Grants = Partner is null ? grants : Array.Empty<(OperationsRoleGrant, OperationsRole)>();
    }

    public OperationsUser User { get; }

    public IReadOnlyList<(OperationsRoleGrant Grant, OperationsRole Role)> Grants { get; }

    public OperationsPartnerScope? Partner { get; }

    public bool IsPartnerAgent => Partner is not null;

    // Money is handled by this person at this warehouse: always for the company's staff, for a partner agent only where allowed (R-8).
    public bool HandlesMoneyAt(OperationsWarehouse warehouse) =>
        Partner is null || ((warehouse ?? throw new ArgumentNullException(nameof(warehouse))).Id == Partner.WarehouseId && Partner.Open && Partner.HandlesMoney);

    public bool IsOwner => User.IsOwner;

    public bool IsAdministrator => User.IsAdministrator;

    // The role Platform knows (design section 3).
    public string PlatformRole => IsOwner ? "owner" : IsAdministrator ? "admin" : "staff";

    public bool Has(string permission) => IsOwner || Grants.Any(item => item.Role.Holds(permission)) || Partner?.Holds(permission) == true;

    public bool HasTenantWide(string permission) =>
        IsOwner || Grants.Any(item => item.Grant.Scope == OperationsRoleScope.Tenant && item.Role.Holds(permission));

    public bool CoversBranch(string permission, Guid branchId) =>
        HasTenantWide(permission) || Grants.Any(item => item.Grant.Scope == OperationsRoleScope.Branch && item.Grant.ScopeId == branchId && item.Role.Holds(permission));

    public bool CoversWarehouse(string permission, Guid warehouseId, Guid branchId) =>
        CoversBranch(permission, branchId) || Grants.Any(item => item.Grant.Scope == OperationsRoleScope.Warehouse && item.Grant.ScopeId == warehouseId && item.Role.Holds(permission))
        || (Partner is { } partner && partner.WarehouseId == warehouseId && partner.Holds(permission));

    // Everything a role holds must be held by the granter in that scope (design section 3).
    public bool CanGrant(OperationsRole role, OperationsRoleScope scope, Guid? scopeId, Guid? branchOfWarehouse)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (role.IsAdministrator)
        {
            return IsOwner;
        }

        if (Partner is not null)
        {
            return false;
        }

        return role.Permissions.All(permission => scope switch
        {
            OperationsRoleScope.Tenant => HasTenantWide(permission),
            OperationsRoleScope.Branch => scopeId is { } branch && CoversBranch(permission, branch),
            _ => scopeId is { } warehouse && branchOfWarehouse is { } parent && CoversWarehouse(permission, warehouse, parent)
        });
    }
}
