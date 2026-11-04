using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Events;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.GetOperationsDiscrepancies;

// =========================================================
// FILE: GetOperationsDiscrepanciesUseCase.cs
//
// PURPOSE:
// What did not match at a destination, for the destination and the origin (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.arrivals.receive at the destination or the origin
// =========================================================
public sealed class GetOperationsDiscrepanciesUseCase
{
    private readonly OperationsArrivalDesk _desk;

    private readonly IOperationsArrivalRepository _arrivals;

    public GetOperationsDiscrepanciesUseCase(
        OperationsArrivalDesk desk,
        IOperationsArrivalRepository arrivals)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _arrivals = arrivals ?? throw new ArgumentNullException(nameof(arrivals));
    }

    public async Task<GetOperationsDiscrepanciesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsDiscrepanciesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.ArrivalsReceive, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsDiscrepanciesResponse.Failure(refused);
        }

        IReadOnlyDictionary<Guid, OperationsWarehouse> all = await _desk.WarehousesAsync(cancellationToken);
        HashSet<Guid> mine = warehouses.Select(item => item.Id).ToHashSet();
        return GetOperationsDiscrepanciesResponse.Success((await _arrivals.ListDiscrepanciesAsync(mine, request.OpenOnly, 300, cancellationToken)).Select(row => new OperationsDiscrepancyRow(
            row.Item.Id, row.Item.Kind, row.Item.Status, row.Item.Code, row.DispatchNumber, all.TryGetValue(row.Item.WarehouseId, out OperationsWarehouse? at) ? at.Code : string.Empty,
            row.Item.CreatedUtc, row.Item.Note, row.Item.ResolvedUtc, row.Item.IsOpen && (mine.Contains(row.Item.WarehouseId) || (row.OriginWarehouseId is { } origin && mine.Contains(origin)))))
            .ToList());
    }
}
