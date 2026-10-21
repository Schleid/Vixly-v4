using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickups;

// =========================================================
// FILE: GetOperationsPickupsUseCase.cs
//
// PURPOSE:
// The shipments ready for pickup at a destination warehouse (Slice 7 step 2, R-9).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.pickup.release, per warehouse
// =========================================================
public sealed class GetOperationsPickupsUseCase
{
    private readonly OperationsPickupDesk _desk;

    private readonly IOperationsPickupRepository _pickups;

    public GetOperationsPickupsUseCase(
        OperationsPickupDesk desk,
        IOperationsPickupRepository pickups)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _pickups = pickups ?? throw new ArgumentNullException(nameof(pickups));
    }

    public async Task<GetOperationsPickupsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsPickupsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsPickupsResponse.Failure(refused);
        }

        OperationsWarehouse here = warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) ?? warehouses[0];
        string? text = string.IsNullOrWhiteSpace(request.Query) ? null : request.Query.Trim();

        if (text is { Length: > 64 })
        {
            text = text[..64];
        }

        IReadOnlyList<(OperationsShipment Shipment, int Ready, DateTimeOffset ReadySince)> ready = await _pickups.ListReadyAsync(here.Id, text, 200, cancellationToken);
        HashSet<Guid> unpaid = (await _pickups.ListInvoicesAsync(ready.Select(item => item.Shipment.Id).ToList(), cancellationToken))
            .Where(invoice => OperationsBundling.Unpaid(new[] { invoice })).Select(invoice => invoice.ShipmentId!.Value).ToHashSet();
        return GetOperationsPickupsResponse.Success(warehouses.Select(item => new OperationsArrivalPlace(item.Id, item.Code, item.Name, item.ClearsCustoms) { TimeZone = item.TimeZone }).ToList(),
            here.Id, ready.Select(item => new OperationsPickupRow(item.Shipment.Id, item.Shipment.Number ?? string.Empty, item.Shipment.RecipientName, item.Shipment.CollectorName, item.Ready,
                item.ReadySince, unpaid.Contains(item.Shipment.Id))).ToList());
    }
}
