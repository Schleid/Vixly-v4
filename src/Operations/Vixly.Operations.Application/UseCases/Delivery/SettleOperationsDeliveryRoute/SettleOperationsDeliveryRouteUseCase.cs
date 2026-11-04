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

namespace Vixly.Operations.Application.UseCases.Delivery.SettleOperationsDeliveryRoute;

// =========================================================
// FILE: SettleOperationsDeliveryRouteUseCase.cs
//
// PURPOSE:
// Settle a route back from its round: the cash counted, once every invoice it collected for is paid in at the cashier (Slice 7 step 3, R-13).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.delivery.plan at its warehouse
// - A difference between the cash collected and the cash handed in needs a note
// =========================================================
public sealed class SettleOperationsDeliveryRouteUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly IOperationsPickupRepository _pickups;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SettleOperationsDeliveryRouteUseCase(
        OperationsDeliveryDesk desk,
        IOperationsPickupRepository pickups,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _pickups = pickups ?? throw new ArgumentNullException(nameof(pickups));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SettleOperationsDeliveryRouteResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SettleOperationsDeliveryRouteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, OperationsDeliveryRouteView? view, bool planner, Error? refused) = await _desk.RouteForAsync(context, request.RouteId, cancellationToken);

        if (refused is not null || view is null)
        {
            return SettleOperationsDeliveryRouteResponse.Failure(refused ?? OperationsDeliveryErrors.NotFound);
        }

        if (!planner)
        {
            return SettleOperationsDeliveryRouteResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        OperationsDeliveryRoute route = view.Route;

        if (route.Status == OperationsRouteStatus.Settled)
        {
            return SettleOperationsDeliveryRouteResponse.Success();
        }

        if (OperationsBundling.Unpaid(await _pickups.ListInvoicesAsync(view.Stops.Where(stop => stop.CashCollected > 0m).Select(stop => stop.ShipmentId).ToList(), cancellationToken)))
        {
            return SettleOperationsDeliveryRouteResponse.Failure(OperationsDeliveryErrors.CashNotPaidIn);
        }

        try
        {
            route.Settle(request.HandedIn, request.Note, access!.User.Id, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return SettleOperationsDeliveryRouteResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.delivery.route_settled", nameof(OperationsDeliveryRoute), route.Id, after: new { route.Number, collected = route.CashCollected, handedIn = route.CashHandedIn,
            note = route.SettleNote });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SettleOperationsDeliveryRouteResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return SettleOperationsDeliveryRouteResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
