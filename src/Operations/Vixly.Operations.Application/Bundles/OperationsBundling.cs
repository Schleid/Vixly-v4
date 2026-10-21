using System.Globalization;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsBundling.cs
//
// PURPOSE:
// The rules and events shared by bundles and repack boxes (Slice 6A, D-3, D-6, D-8 and D-9).
//
// RESPONSIBILITIES:
// - Say whether a parcel may be packed: final, clear of holds and inspections
// - Say whether its shipment is paid (F-3)
// - Write the packed and boxed events, and void them when a parcel comes out
//
// IMPORTANT NOTES:
// - The caller saves; a refused event abandons the caller's save rather than keep a parcel in a bundle without its event
// - A shipment made final before billing has no invoice and passes (D-3)
// =========================================================
public sealed class OperationsBundling
{
    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsTracker _tracker;

    private readonly TimeProvider _timeProvider;

    public OperationsBundling(IOperationsBillingRepository billing, IOperationsTrackingRepository tracking, OperationsTracker tracker, TimeProvider timeProvider)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    // Why the parcel cannot be packed yet: not in a final shipment (or no longer travelling), on hold or waiting for an inspection; none when it can.
    public static Vixly.Shared.Kernel.Errors.Error? Ready(OperationsParcel parcel, OperationsShipment? shipment)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        if (shipment is not { IsBooked: true } || parcel.ShipmentId != shipment.Id || parcel.Milestone is not (>= OperationsMilestone.Booked and < OperationsMilestone.Manifested))
        {
            return OperationsBundleErrors.NotFinal;
        }

        return parcel.State.IsBlocked || parcel.HoldReasons != OperationsHoldReasons.None || parcel.Flags.HasFlag(OperationsParcelFlags.AwaitingCheck) ? OperationsBundleErrors.Blocked : null;
    }

    // Still to pay, waiting for finance or for the credit department: held from bundling (F-3). Charged to a credit account, it is released (K-4); a voided invoice asks nothing.
    public static bool Unpaid(IEnumerable<Vixly.Operations.Domain.Billing.OperationsInvoice> invoices) =>
        invoices.Any(invoice => invoice.PaymentState is OperationsPaymentState.Unpaid or OperationsPaymentState.AwaitingConfirmation or OperationsPaymentState.AwaitingCredit);

    public async Task<bool> PaidAsync(OperationsShipment shipment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        return !Unpaid(await _billing.ListShipmentInvoicesAsync(new[] { shipment.Id }, cancellationToken));
    }

    public static decimal VolumeCm3(OperationsParcel parcel) => (parcel.LengthCm ?? 0m) * (parcel.WidthCm ?? 0m) * (parcel.HeightCm ?? 0m);

    public async Task PackedAsync(OperationsParcel parcel, OperationsBundle bundle, Guid userId, string scanKey, CancellationToken cancellationToken) =>
        await RecordAsync(new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.Packed,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["bundle"] = bundle.Number }, _timeProvider.GetUtcNow(), await FreshKeyAsync("pack:" + parcel.Id.ToString("N") + ":", scanKey, cancellationToken),
            OperationsTrackingActor.Staff, OperationsTrackingSource.Scan) { ActorUserId = userId }, cancellationToken);

    public async Task BoxedAsync(OperationsParcel parcel, OperationsRepackBox box, Guid userId, string scanKey, CancellationToken cancellationToken) =>
        await RecordAsync(new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.Boxed,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["box"] = box.Number }, _timeProvider.GetUtcNow(), await FreshKeyAsync("box:" + parcel.Id.ToString("N") + ":", scanKey, cancellationToken),
            OperationsTrackingActor.Staff, OperationsTrackingSource.Scan) { ActorUserId = userId }, cancellationToken);

    // The caller has already answered a true repeat (the parcel is in already); a key used before means a stale form posted after a take-out: a new key, so the event is written (review).
    private async Task<string> FreshKeyAsync(string prefix, string scanKey, CancellationToken cancellationToken) =>
        await _tracking.FindByKeyAsync(prefix + scanKey, cancellationToken) is null ? prefix + scanKey : prefix + Guid.NewGuid().ToString("N");

    // On the bundle itself: made, closed, voided (ADR-0021); once each, so the key is the bundle and the code.
    public Task BundleEventAsync(OperationsBundle bundle, string code, IReadOnlyDictionary<string, string> arguments, Guid userId, string? note, CancellationToken cancellationToken) =>
        RecordAsync(new OperationsTrackingDraft(OperationsTrackingSubject.Bundle, bundle.Id, code, arguments, _timeProvider.GetUtcNow(),
            "bundle:" + bundle.Id.ToString("N") + ":" + code[(code.LastIndexOf('.') + 1)..], OperationsTrackingActor.Staff, OperationsTrackingSource.Manual) { ActorUserId = userId, Note = note },
            cancellationToken);

    public static string Kilograms(decimal kilograms) => kilograms.ToString("0.###", CultureInfo.InvariantCulture) + " kg";

    public static string Size(decimal length, decimal width, decimal height) =>
        string.Create(CultureInfo.InvariantCulture, $"{length:0.#}x{width:0.#}x{height:0.#} cm");

    // The parcel came out (taken out, or its bundle or box voided): its last event of this code is voided, so its milestone goes back (D-6, D-8).
    public async Task VoidLatestAsync(OperationsParcel parcel, string code, string why, Guid userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        IReadOnlyList<OperationsTrackingEvent> history = await _tracking.ListForSubjectAsync(OperationsTrackingSubject.Parcel, parcel.Id, cancellationToken);
        HashSet<Guid> voided = history.Where(item => item.VoidsEventId is not null).Select(item => item.VoidsEventId!.Value).ToHashSet();

        if (history.Where(item => item.Code == code && item.VoidsEventId is null && !voided.Contains(item.Id))
            .OrderByDescending(item => item.OccurredUtc).ThenByDescending(item => item.CreatedUtc).FirstOrDefault() is not { } target)
        {
            return;
        }

        await RecordAsync(new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.EventVoided,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["why"] = why }, _timeProvider.GetUtcNow(), "void:" + target.Id.ToString("N"),
            OperationsTrackingActor.Staff, OperationsTrackingSource.Manual) { ActorUserId = userId, VoidsEventId = target.Id }, cancellationToken);
    }

    private async Task RecordAsync(OperationsTrackingDraft draft, CancellationToken cancellationToken)
    {
        if ((await _tracker.RecordAsync(draft, cancellationToken)).Error is { } refused)
        {
            throw new InvalidOperationException("The " + draft.Code + " event of " + draft.SubjectId + " was refused: " + refused.Code);
        }
    }
}
