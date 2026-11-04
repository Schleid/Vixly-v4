using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsStorageLocations;

// =========================================================
// FILE: ListOperationsStorageLocationsUseCase.cs
//
// PURPOSE:
// The shelves of one warehouse (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.warehouses.manage for that warehouse
// =========================================================
public sealed class ListOperationsStorageLocationsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    public ListOperationsStorageLocationsUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
    }

    public async Task<ListOperationsStorageLocationsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsStorageLocationsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsStorageLocationsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _scope.CoveredAsync(access, OperationsPermissions.WarehousesManage, request.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return ListOperationsStorageLocationsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        IReadOnlyList<OperationsStorageLocation> locations = (await _receiving.ListLocationsAsync(warehouse.Id, cancellationToken)).OrderBy(location => location.Code, StringComparer.Ordinal).ToList();
        return ListOperationsStorageLocationsResponse.Success(warehouse, locations);
    }
}
