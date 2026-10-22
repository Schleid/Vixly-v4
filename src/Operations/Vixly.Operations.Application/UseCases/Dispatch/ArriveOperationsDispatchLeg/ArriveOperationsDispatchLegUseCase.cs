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

namespace Vixly.Operations.Application.UseCases.Dispatch.ArriveOperationsDispatchLeg;

// =========================================================
// FILE: ArriveOperationsDispatchLegUseCase.cs
//
// PURPOSE:
// Record that a leg arrived, at the time it did (Slice 6B, D-4 and D-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// - The last leg arriving brings every parcel to the destination country (customer); the destination's receiving scan follows (Slice 7)
// =========================================================
public sealed class ArriveOperationsDispatchLegUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly OperationsDispatching _dispatching;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ArriveOperationsDispatchLegUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        OperationsDispatching dispatching,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _dispatching = dispatching ?? throw new ArgumentNullException(nameof(dispatching));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ArriveOperationsDispatchLegResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ArriveOperationsDispatchLegRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return ArriveOperationsDispatchLegResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return ArriveOperationsDispatchLegResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        if (dispatch.Legs.FirstOrDefault(item => item.Id == request.LegId) is not { } leg)
        {
            return ArriveOperationsDispatchLegResponse.Failure(OperationsDispatchErrors.LegNotFound);
        }

        // Arrived already (a double click): done.
        if (leg.Status == OperationsDispatchLegStatus.Arrived)
        {
            return ArriveOperationsDispatchLegResponse.Success();
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        bool last;

        try
        {
            last = dispatch.ArriveLeg(leg.Id, request.At is { } at ? OperationsTimeZones.At(at, settings.TimeZone) : utcNow, utcNow);
        }
        catch (DomainException exception)
        {
            return ArriveOperationsDispatchLegResponse.Failure(exception.Error);
        }

        await _dispatching.ArrivedAsync(dispatch, leg, last, last ? await _dispatching.ParcelsAsync(dispatch, cancellationToken) : Array.Empty<Vixly.Operations.Domain.Tracking.OperationsParcel>(),
            access!.User.Id, cancellationToken);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ArriveOperationsDispatchLegResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return ArriveOperationsDispatchLegResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
