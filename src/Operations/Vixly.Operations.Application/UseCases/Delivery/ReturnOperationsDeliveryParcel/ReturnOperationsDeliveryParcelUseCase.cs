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

namespace Vixly.Operations.Application.UseCases.Delivery.ReturnOperationsDeliveryParcel;

// =========================================================
// FILE: ReturnOperationsDeliveryParcelUseCase.cs
//
// PURPOSE:
// A parcel that could not be delivered leaves for its origin: returned to the sender (Slice 7 step 3, R-14).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.delivery.plan at the warehouse
// - Only a parcel going back after its last failed delivery
// =========================================================
public sealed class ReturnOperationsDeliveryParcelUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly IOperationsDeliveryRepository _routes;

    private readonly OperationsTracker _tracker;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ReturnOperationsDeliveryParcelUseCase(
        OperationsDeliveryDesk desk,
        IOperationsDeliveryRepository routes,
        OperationsTracker tracker,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ReturnOperationsDeliveryParcelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ReturnOperationsDeliveryParcelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.DeliveryPlan, cancellationToken);

        if (refused is not null)
        {
            return ReturnOperationsDeliveryParcelResponse.Failure(refused);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } here
            || (await _routes.ListReturnsAsync(here.Id, 500, cancellationToken)).FirstOrDefault(item => item.Parcel.Id == request.ParcelId) is not { Parcel: { } parcel })
        {
            return ReturnOperationsDeliveryParcelResponse.Failure(OperationsDeliveryErrors.NotFound);
        }

        string? note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        if (note is { Length: > OperationsDeliveryStop.NoteMaxLength } || (note is not null && (note.Any(char.IsControl) || note.IndexOfAny(['<', '>']) >= 0)))
        {
            return ReturnOperationsDeliveryParcelResponse.Failure(OperationsDeliveryErrors.ReasonRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (await _tracker.RecordManyAsync(new[] { (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.ReturnedToSender,
            new Dictionary<string, string>(StringComparer.Ordinal), utcNow, "returned:" + parcel.Id.ToString("N"), OperationsTrackingActor.Staff, OperationsTrackingSource.Manual)
            { ActorUserId = access!.User.Id, Note = note }, parcel) }, cancellationToken) is { } failed)
        {
            _unitOfWork.DiscardChanges();
            return ReturnOperationsDeliveryParcelResponse.Failure(failed);
        }

        _audit.Record(access!.User.Id, "ops.delivery.returned", "OperationsParcel", parcel.Id, after: new { parcel.Number, note });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ReturnOperationsDeliveryParcelResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return ReturnOperationsDeliveryParcelResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
