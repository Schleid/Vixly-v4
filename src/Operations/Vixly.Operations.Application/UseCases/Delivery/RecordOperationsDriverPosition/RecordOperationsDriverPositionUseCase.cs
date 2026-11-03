using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Delivery;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.RecordOperationsDriverPosition;

// =========================================================
// FILE: RecordOperationsDriverPositionUseCase.cs
//
// PURPOSE:
// Keeps the position the driver's phone sends while the route is out (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only the route's driver, only while the route is out
// - No audit entry: positions are kept on their own, 7 days
// =========================================================
public sealed class RecordOperationsDriverPositionUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly IOperationsDriverTrackingRepository _tracking;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RecordOperationsDriverPositionUseCase(
        OperationsDeliveryDesk desk,
        IOperationsDriverTrackingRepository tracking,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RecordOperationsDriverPositionResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RecordOperationsDriverPositionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, OperationsDeliveryRouteView? view, bool _, Error? refused) = await _desk.RouteForAsync(context, request.RouteId, cancellationToken);

        if (refused is not null || view is null)
        {
            return RecordOperationsDriverPositionResponse.Failure(refused ?? OperationsDeliveryErrors.NotFound);
        }

        if (view.Route.DriverUserId != access!.User.Id || view.Route.Status != OperationsRouteStatus.Out)
        {
            return RecordOperationsDriverPositionResponse.Failure(OperationsDeliveryErrors.NotTheDriver);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        // One position every 10 seconds is plenty; more are answered as kept and dropped.
        if (await _tracking.LatestAsync(view.Route.Id, cancellationToken) is { } latest && utcNow - latest.CreatedUtc < TimeSpan.FromSeconds(10))
        {
            return RecordOperationsDriverPositionResponse.Success(false);
        }

        try
        {
            _tracking.Add(OperationsDriverPosition.Take(context.RequiredTenantId, view.Route.Id, access.User.Id, request.Latitude, request.Longitude, request.AccuracyMeters,
                request.TakenUtc ?? utcNow, utcNow));
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return RecordOperationsDriverPositionResponse.Failure(exception.Error);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RecordOperationsDriverPositionResponse.Success(true)
            : RecordOperationsDriverPositionResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
