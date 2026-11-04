using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsShelfCounts;

// =========================================================
// FILE: GetOperationsShelfCountsUseCase.cs
//
// PURPOSE:
// The shelf counts of the warehouses a person may count, or one of them (Slice 12D, owner H-7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only warehouses where the person may receive
// =========================================================
public sealed class GetOperationsShelfCountsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsShelfCountRepository _counts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public GetOperationsShelfCountsUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsShelfCountRepository counts,
        IOperationsReceivingRepository receiving,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _counts = counts ?? throw new ArgumentNullException(nameof(counts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsShelfCountsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsShelfCountsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsShelfCountsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.ReceivingReceive, false, cancellationToken);

        if (warehouses.Count == 0)
        {
            return GetOperationsShelfCountsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        IReadOnlyList<OperationsShelfCount> counts = request.CountId is { } id
            ? (await _counts.GetAsync(id, cancellationToken) is { } one && warehouses.Any(item => item.Id == one.WarehouseId) ? new[] { one } : Array.Empty<OperationsShelfCount>())
            : await _counts.ListAsync(warehouses.Select(item => item.Id).ToList(), 30, cancellationToken);

        if (request.CountId is not null && counts.Count == 0)
        {
            return GetOperationsShelfCountsResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        List<OperationsShelfCountView> views = new();

        foreach (OperationsShelfCount count in counts)
        {
            int expected = count.IsOpen && request.CountId is not null
                ? (await _receiving.ListParcelsAsync(new OperationsParcelFilter(new[] { count.WarehouseId }, null, false, false, true, count.LocationId), 1, 1, cancellationToken)).TotalCount
                : OperationsShelfCount.Lines(count.Found).Count + OperationsShelfCount.Lines(count.Missing).Count;
            views.Add(new OperationsShelfCountView(count.Id, count.WarehouseId, warehouses.First(item => item.Id == count.WarehouseId).Name, count.LocationCode, count.IsOpen,
                count.CreatedUtc, count.FinishedUtc, count.ScannedCodes, expected, OperationsShelfCount.Lines(count.Found), OperationsShelfCount.Lines(count.Missing),
                OperationsShelfCount.Lines(count.Extra), OperationsShelfCount.Lines(count.Unknown)));
        }

        return GetOperationsShelfCountsResponse.Success(views, warehouses);
    }
}
