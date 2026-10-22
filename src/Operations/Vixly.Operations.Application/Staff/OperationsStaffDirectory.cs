using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Staff;

// =========================================================
// FILE: OperationsStaffDirectory.cs
//
// PURPOSE:
// Builds staff read models with their roles and scope names (Slice 1D).
//
// RESPONSIBILITIES:
// - Join users, active grants, roles, branches and warehouses for the screens
//
// IMPORTANT NOTES:
// - Read only
// =========================================================
public sealed class OperationsStaffDirectory
{
    private readonly IOperationsRoleRepository _roles;

    private readonly IOperationsLocationRepository _locations;

    public OperationsStaffDirectory(IOperationsRoleRepository roles, IOperationsLocationRepository locations)
    {
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    public async Task<IReadOnlyList<OperationsStaffMember>> DescribeAsync(IReadOnlyList<OperationsUser> users, IReadOnlyList<OperationsRoleGrant> grants, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(grants);

        Dictionary<Guid, OperationsRole> roles = (await _roles.ListAsync(cancellationToken)).ToDictionary(role => role.Id);
        Dictionary<Guid, string> places = (await _locations.ListBranchesAsync(cancellationToken)).ToDictionary(branch => branch.Id, branch => branch.Name);

        foreach (Vixly.Operations.Domain.Locations.OperationsWarehouse warehouse in await _locations.ListWarehousesAsync(cancellationToken))
        {
            places[warehouse.Id] = warehouse.Name;
        }

        return users.Select(user => new OperationsStaffMember(
                user.Id, user.FullName, user.Email.Value, user.Account.Status, user.Account.StatusReason, user.IsOwner, user.IsAdministrator, user.HasSecondFactor, user.Account.LastSignInUtc,
                grants.Where(grant => grant.UserId == user.Id && grant.IsActive && roles.ContainsKey(grant.RoleId))
                    .Select(grant => new OperationsStaffGrant(grant.Id, grant.RoleId, roles[grant.RoleId].Name, roles[grant.RoleId].IsAdministrator, grant.Scope, grant.ScopeId,
                        grant.ScopeId is { } scope && places.TryGetValue(scope, out string? name) ? name : null, grant.GrantedUtc))
                    .ToList(),
                user.PartnerWarehouseId is { } partner ? places.GetValueOrDefault(partner, string.Empty) : null))
            .ToList();
    }
}
