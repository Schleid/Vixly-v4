using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.GetOperationsArrival;

// =========================================================
// FILE: GetOperationsArrivalUseCase.cs
//
// PURPOSE:
// One dispatch received at a destination warehouse: its bundles and what did not match (Slice 7, R-1 and R-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.arrivals.receive at the warehouse it is bound for (R-11)
// =========================================================
public sealed class GetOperationsArrivalUseCase
{
    private readonly OperationsArrivalDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsArrivalRepository _arrivals;

    public GetOperationsArrivalUseCase(
        OperationsArrivalDesk desk,
        IOperationsDispatchRepository dispatches,
        IOperationsArrivalRepository arrivals)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _arrivals = arrivals ?? throw new ArgumentNullException(nameof(arrivals));
    }

    public async Task<GetOperationsArrivalResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsArrivalRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenEitherAsync(context, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsArrivalResponse.Failure(refused);
        }

        (OperationsDispatch? dispatch, OperationsWarehouse? here) = await _desk.DispatchAsync(request.DispatchId, request.WarehouseId, warehouses, cancellationToken);

        if (dispatch is null || here is null)
        {
            return GetOperationsArrivalResponse.Failure(OperationsArrivalErrors.NotFound);
        }

        IReadOnlyDictionary<Guid, OperationsWarehouse> all = await _desk.WarehousesAsync(cancellationToken);
        IReadOnlyList<OperationsParcel> parcels = await _dispatches.ListDispatchParcelsAsync(dispatch.Id, false, cancellationToken);
        Dictionary<Guid, DateTimeOffset?> arrived = (await _dispatches.ListLinksAsync(dispatch.Id, cancellationToken)).ToDictionary(link => link.BundleId, link => link.ArrivedUtc);
        bool canReceive = OperationsBundleDesk.Can(access!, OperationsPermissions.ArrivalsReceive, here);
        List<OperationsArrivalBundleRow> bundles = (await _dispatches.ListDispatchBundlesAsync(dispatch.Id, cancellationToken)).Select(bundle => new OperationsArrivalBundleRow(bundle.Id,
            bundle.Number, bundle.DestinationWarehouseId is { } to && all.TryGetValue(to, out OperationsWarehouse? target) ? target.Code : bundle.DestinationCountryCode,
            OperationsArrivalDesk.EndsAt(here, bundle), arrived.GetValueOrDefault(bundle.Id), bundle.OpenedUtc, arrived.GetValueOrDefault(bundle.Id) is not null,
            parcels.Count(parcel => parcel.BundleId == bundle.Id), parcels.Count(parcel => parcel.BundleId == bundle.Id && parcel.WarehouseId == here.Id))).ToList();
        List<OperationsDiscrepancyRow> discrepancies = (await _arrivals.ListDispatchDiscrepanciesAsync(dispatch.Id, cancellationToken)).Select(item => new OperationsDiscrepancyRow(item.Id,
            item.Kind, item.Status, item.Code, dispatch.Number, all.TryGetValue(item.WarehouseId, out OperationsWarehouse? at) ? at.Code : string.Empty, item.CreatedUtc, item.Note,
            item.ResolvedUtc, item.IsOpen && canReceive)).ToList();
        return GetOperationsArrivalResponse.Success(dispatch.Id, dispatch.Number, all.TryGetValue(dispatch.WarehouseId, out OperationsWarehouse? from) ? from.Code : string.Empty,
            dispatch.Status, dispatch.Onward, dispatch.ReceivedUtc, new OperationsArrivalPlace(here.Id, here.Code, here.Name, here.ClearsCustoms) { TimeZone = here.TimeZone },
            here.ClearsCustoms && OperationsBundleDesk.Can(access!, OperationsPermissions.CustomsRecord, here), canReceive, bundles, discrepancies);
    }
}
