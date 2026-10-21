using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Shipments.ListOperationsShipments;

// =========================================================
// FILE: ListOperationsShipmentsUseCase.cs
//
// PURPOSE:
// The shipments desk: the shipments of the warehouses the staff member may see, by origin (Slice 5A).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.shipments.view for the warehouses shown (origins)
// =========================================================
public sealed class ListOperationsShipmentsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    public ListOperationsShipmentsUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
    }

    public async Task<ListOperationsShipmentsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsShipmentsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsShipmentsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.ShipmentsView, false, cancellationToken);

        if (warehouses.Count == 0)
        {
            return ListOperationsShipmentsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        List<Guid> shown = warehouses.Where(warehouse => request.WarehouseId is null || warehouse.Id == request.WarehouseId).Select(warehouse => warehouse.Id).ToList();
        OperationsShipmentFilter filter = new(shown, request.View, Trimmed(request.ServiceCode, 40), Trimmed(request.CountryCode, 2)?.ToUpperInvariant(), Trimmed(request.Search, 60));
        (IReadOnlyList<OperationsShipmentRow> rows, int total) = await _receiving.ListShipmentsAsync(filter, request.Export ? 1 : Math.Max(request.Page, 1),
            request.Export ? MaxExport : PageSize, cancellationToken);
        return ListOperationsShipmentsResponse.Success(rows, total, await _receiving.CountShipmentsAsync(filter, cancellationToken), warehouses);
    }

    public const int PageSize = 25;

    public const int MaxExport = 5000;

    private static string? Trimmed(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length > max ? value.Trim()[..max] : value.Trim();
}
