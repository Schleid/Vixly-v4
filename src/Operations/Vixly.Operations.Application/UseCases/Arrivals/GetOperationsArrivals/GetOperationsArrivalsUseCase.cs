using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.GetOperationsArrivals;

// =========================================================
// FILE: GetOperationsArrivalsUseCase.cs
//
// PURPOSE:
// The dispatches a destination warehouse receives (Slice 7, R-1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.arrivals.receive, per warehouse (R-1)
// - Departed or arrived dispatches bound here, then the last ones received
// =========================================================
public sealed class GetOperationsArrivalsUseCase
{
    private readonly OperationsArrivalDesk _desk;

    private readonly IOperationsArrivalRepository _arrivals;

    public GetOperationsArrivalsUseCase(
        OperationsArrivalDesk desk,
        IOperationsArrivalRepository arrivals)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _arrivals = arrivals ?? throw new ArgumentNullException(nameof(arrivals));
    }

    public async Task<GetOperationsArrivalsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsArrivalsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (_, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenEitherAsync(context, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsArrivalsResponse.Failure(refused);
        }

        OperationsWarehouse here = warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) ?? warehouses[0];
        IReadOnlyDictionary<Guid, OperationsWarehouse> all = await _desk.WarehousesAsync(cancellationToken);
        // Filtered by the warehouse in the database (review): the oldest coming first, the last 30 received.
        List<OperationsDispatch> coming = (await _arrivals.ListArrivingAsync(here.Id, here.Address.CountryCode, false, 200, cancellationToken)).ToList();
        List<OperationsDispatch> received = (await _arrivals.ListArrivingAsync(here.Id, here.Address.CountryCode, true, 30, cancellationToken)).ToList();
        return GetOperationsArrivalsResponse.Success(warehouses.Select(item => new OperationsArrivalPlace(item.Id, item.Code, item.Name, item.ClearsCustoms) { TimeZone = item.TimeZone }).ToList(), here.Id,
            coming.Select(item => Row(item, all)).ToList(), received.Select(item => Row(item, all)).ToList());
    }

    private static OperationsArrivalRow Row(OperationsDispatch dispatch, IReadOnlyDictionary<Guid, OperationsWarehouse> all) =>
        new(dispatch.Id, dispatch.Number, all.TryGetValue(dispatch.WarehouseId, out OperationsWarehouse? from) ? from.Code : string.Empty, dispatch.Status, dispatch.DepartedUtc,
            dispatch.ArrivedUtc, dispatch.BundleCount, dispatch.Onward, dispatch.ReceivedUtc);
}
