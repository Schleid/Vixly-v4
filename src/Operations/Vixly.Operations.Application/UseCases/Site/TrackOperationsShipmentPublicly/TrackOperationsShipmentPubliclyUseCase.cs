using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.UseCases.Site.TrackOperationsShipmentPublicly;

// =========================================================
// FILE: TrackOperationsShipmentPubliclyUseCase.cs
//
// PURPOSE:
// Public tracking by number: milestones and dates only (Slice 1B, owner decision; ADR-0021).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
// - Owner, 2026-10-28: by the shipment's tracking number (VX 482 917 36 054), given once it is paid, on account or collect;
//   the milestones its parcels reached, each with the first time one of them reached it
//
// IMPORTANT NOTES:
// - Never amounts, names, addresses, photos or notes
// - Unknown and other-tenant numbers give the same answer
// =========================================================
public sealed class TrackOperationsShipmentPubliclyUseCase
{
    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    public TrackOperationsShipmentPubliclyUseCase(IOperationsReceivingRepository receiving, IOperationsTrackingRepository tracking)
    {
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
    }

    public async Task<TrackOperationsShipmentPubliclyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        TrackOperationsShipmentPubliclyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // A mistyped number fails its check digit and is never looked up.
        if (OperationsTrackingNumbers.Read(request.Number) is not { } number
            || await _receiving.FindShipmentByTrackingNumberAsync(number, cancellationToken) is not { } shipment)
        {
            return TrackOperationsShipmentPubliclyResponse.Failure(OperationsCommonErrors.NotFound);
        }

        List<Guid> parcels = (await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken)).Select(item => item.Id).ToList();
        List<OperationsTrackingEvent> events = new(await _tracking.ListForSubjectAsync(OperationsTrackingSubject.Shipment, shipment.Id, cancellationToken));

        if (parcels.Count > 0)
        {
            events.AddRange(await _tracking.ListForSubjectsAsync(OperationsTrackingSubject.Parcel, parcels, cancellationToken));
        }

        List<OperationsPublicMilestone> milestones = OperationsTrackingViews.ForPublic(events)
            .Select(item => new OperationsPublicMilestone(item.Milestone.ToString(), item.OccurredUtc, null))
            .ToList();

        return TrackOperationsShipmentPubliclyResponse.Success(milestones);
    }
}
