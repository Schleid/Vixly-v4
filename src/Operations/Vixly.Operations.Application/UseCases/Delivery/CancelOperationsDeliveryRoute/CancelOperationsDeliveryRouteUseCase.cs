using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Application.Events;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.CancelOperationsDeliveryRoute;

// =========================================================
// FILE: CancelOperationsDeliveryRouteUseCase.cs
//
// PURPOSE:
// Cancel a planned route: its shipments wait for another (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.delivery.plan at its warehouse
// - Planned routes only
// =========================================================
public sealed class CancelOperationsDeliveryRouteUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CancelOperationsDeliveryRouteUseCase(
        OperationsDeliveryDesk desk,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CancelOperationsDeliveryRouteResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CancelOperationsDeliveryRouteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, OperationsDeliveryRouteView? view, bool planner, Error? refused) = await _desk.RouteForAsync(context, request.RouteId, cancellationToken);

        if (refused is not null || view is null)
        {
            return CancelOperationsDeliveryRouteResponse.Failure(refused ?? OperationsDeliveryErrors.NotFound);
        }

        if (!planner)
        {
            return CancelOperationsDeliveryRouteResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            view.Route.Cancel(request.Reason, utcNow);
        }
        catch (DomainException exception)
        {
            return CancelOperationsDeliveryRouteResponse.Failure(exception.Error);
        }

        foreach (OperationsParcel parcel in view.Parcels)
        {
            parcel.OffRoute(false, utcNow);
        }

        _audit.Record(access!.User.Id, "ops.delivery.route_cancelled", nameof(OperationsDeliveryRoute), view.Route.Id, after: new { view.Route.Number, reason = view.Route.CancelReason });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CancelOperationsDeliveryRouteResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return CancelOperationsDeliveryRouteResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
