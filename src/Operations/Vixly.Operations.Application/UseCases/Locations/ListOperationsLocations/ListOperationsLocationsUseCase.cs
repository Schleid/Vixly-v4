using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.Locations.ListOperationsLocations;

// =========================================================
// FILE: ListOperationsLocationsUseCase.cs
//
// PURPOSE:
// Branches, warehouses and country contacts for the Owner editor (Slice 1B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Owner only until Slice 1C
// =========================================================
public sealed class ListOperationsLocationsUseCase
{
    private readonly IOperationsLocationRepository _locations;

    private readonly OperationsOwnerCheck _owner;

    private readonly IOperationsUserRepository _users;

    public ListOperationsLocationsUseCase(
        IOperationsLocationRepository locations,
        OperationsOwnerCheck owner,
        IOperationsUserRepository users)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public async Task<ListOperationsLocationsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsLocationsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsLocationsResponse.Failure(Vixly.Operations.Application.Security.OperationsAccessErrors.SignInRequired);
        }

        if (!access.Has(Vixly.Operations.Domain.Access.OperationsPermissions.BranchesView) && !access.Has(Vixly.Operations.Domain.Access.OperationsPermissions.WarehousesView))
        {
            return ListOperationsLocationsResponse.Failure(Vixly.Operations.Application.Security.OperationsAccessErrors.PermissionRequired);
        }

        // Each user sees the branches and warehouses of their scope only (design section 3, fix 3).
        return ListOperationsLocationsResponse.Success(
            (await _locations.ListBranchesAsync(cancellationToken)).Where(branch => access.CoversBranch(Vixly.Operations.Domain.Access.OperationsPermissions.BranchesView, branch.Id)).ToList(),
            (await _locations.ListWarehousesAsync(cancellationToken)).Where(warehouse => access.CoversWarehouse(Vixly.Operations.Domain.Access.OperationsPermissions.WarehousesView, warehouse.Id, warehouse.BranchId)).ToList(),
            access.HasTenantWide(Vixly.Operations.Domain.Access.OperationsPermissions.BranchesView) ? await _locations.ListContactsAsync(cancellationToken) : Array.Empty<OperationsCountryContact>(),
            (await _users.ListAsync(null, 1, 200, cancellationToken)).Items.Where(user => user.Account.IsActive)
                .Select(user => new Vixly.Operations.Application.Shipments.OperationsChoice(user.Id, user.FullName, null)).ToList());
    }
}
