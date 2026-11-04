using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Holds.StartOperationsStockCount;

// =========================================================
// FILE: StartOperationsStockCountUseCase.cs
//
// PURPOSE:
// Starts a count of the undeclared parcels area of a warehouse: every package nobody could place, with its shelf (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.assign for the warehouse
// =========================================================
public sealed class StartOperationsStockCountUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsStockCountRepository _counts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public StartOperationsStockCountUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsStockCountRepository counts,
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

    public async Task<StartOperationsStockCountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        StartOperationsStockCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return StartOperationsStockCountResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _scope.CoveredAsync(access, OperationsPermissions.ParcelsAssign, request.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return StartOperationsStockCountResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // One open count per warehouse: the one already open is continued.
        if (await _counts.FindOpenAsync(warehouse.Id, cancellationToken) is { } open)
        {
            return StartOperationsStockCountResponse.Success(open.Id);
        }

        Dictionary<Guid, string> shelves = (await _receiving.ListLocationsAsync(warehouse.Id, cancellationToken)).ToDictionary(item => item.Id, item => item.Code);
        IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel> parcels = await _counts.ListUndeclaredAsync(warehouse.Id, OperationsStockCount.MaxLines, cancellationToken);
        OperationsStockCount count = OperationsStockCount.Start(context.RequiredTenantId, warehouse.Id, access.User.Id,
            parcels.Where(parcel => parcel.Number is not null).Select(parcel => (parcel.Id, parcel.Number!, parcel.StorageLocationId is { } shelf ? shelves.GetValueOrDefault(shelf) : null)),
            _timeProvider.GetUtcNow());
        _counts.Add(count);
        _audit.Record(access.User.Id, "ops.holds.count_started", nameof(OperationsStockCount), count.Id, after: new { warehouse = warehouse.Code, parcels = count.Lines.Count });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return StartOperationsStockCountResponse.Success(count.Id);
        }

        // Started at the same moment by another clerk: that one.
        _unitOfWork.DiscardChanges();
        return await _counts.FindOpenAsync(warehouse.Id, cancellationToken) is { } raced
            ? StartOperationsStockCountResponse.Success(raced.Id)
            : StartOperationsStockCountResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
