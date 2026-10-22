using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Dispatch;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.DepartOperationsDispatchLeg;

// =========================================================
// FILE: DepartOperationsDispatchLegUseCase.cs
//
// PURPOSE:
// Record that a leg left, at the time it did (Slice 6B, D-4 and D-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// - The first leg leaving departs every parcel from the origin; a later one puts them in transit (customer)
// =========================================================
public sealed class DepartOperationsDispatchLegUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly OperationsDispatching _dispatching;

    private readonly OperationsDispatchCosting _costing;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public DepartOperationsDispatchLegUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        OperationsDispatching dispatching,
        OperationsDispatchCosting costing,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _dispatching = dispatching ?? throw new ArgumentNullException(nameof(dispatching));
        _costing = costing ?? throw new ArgumentNullException(nameof(costing));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DepartOperationsDispatchLegResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        DepartOperationsDispatchLegRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return DepartOperationsDispatchLegResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return DepartOperationsDispatchLegResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        if (dispatch.Legs.FirstOrDefault(item => item.Id == request.LegId) is not { } leg)
        {
            return DepartOperationsDispatchLegResponse.Failure(OperationsDispatchErrors.LegNotFound);
        }

        // Left already (a double click): done.
        if (leg.Status != OperationsDispatchLegStatus.Planned)
        {
            return DepartOperationsDispatchLegResponse.Success();
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        bool first;
        IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel> parcels = await _dispatching.ParcelsAsync(dispatch, cancellationToken);

        // Nothing on hold leaves the origin (review).
        if (dispatch.Status == OperationsDispatchStatus.Closed && parcels.Any(OperationsDispatching.Held))
        {
            return DepartOperationsDispatchLegResponse.Failure(OperationsDispatchErrors.ParcelHeld);
        }

        try
        {
            first = dispatch.DepartLeg(leg.Id, request.At is { } at ? OperationsTimeZones.At(at, settings.TimeZone) : utcNow, utcNow);
        }
        catch (DomainException exception)
        {
            return DepartOperationsDispatchLegResponse.Failure(exception.Error);
        }

        await _dispatching.DepartedAsync(dispatch, leg, first, parcels, access!.User.Id, cancellationToken);

        // Step 2: the carrier's cost is worked out when the dispatch leaves (D-7).
        if (first)
        {
            await _costing.CostAsync(dispatch, settings, utcNow, cancellationToken);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return DepartOperationsDispatchLegResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return DepartOperationsDispatchLegResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
