using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Inspection;

// =========================================================
// FILE: OperationsInspectionSelection.cs
//
// PURPOSE:
// Chooses parcels for compliance inspection: the company's rules, the random share, a clerk's flag (Slice 3C design section 4).
//
// RESPONSIBILITIES:
// - Say why a parcel goes to inspection at the station, or that it does not
// - Check a parcel again when its customer declares it or says where it goes
// - Open the inspection record and record "inspection required" with the reason
//
// IMPORTANT NOTES:
// - The random draw happens once, at the first check; rules are checked each time until the parcel is chosen
// - A parcel is chosen once: never while it has, or has had, an inspection
// - Its destination comes from the shipment the customer created (Q-C1)
// =========================================================
public sealed class OperationsInspectionSelection
{
    private readonly IOperationsInspectionRepository _inspections;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsInspectionDraw _draw;

    private readonly OperationsTracker _tracker;

    public OperationsInspectionSelection(
        IOperationsInspectionRepository inspections,
        IOperationsReceivingRepository receiving,
        IOperationsSettingsRepository settings,
        OperationsInspectionDraw draw,
        OperationsTracker tracker)
    {
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _draw = draw ?? throw new ArgumentNullException(nameof(draw));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
    }

    // At the station: a flag first, then the rules, then (the first time only) the random share. Null: not chosen.
    public async Task<bool> WasChosenAsync(OperationsParcel parcel, CancellationToken cancellationToken) =>
        parcel.Flags.HasFlag(OperationsParcelFlags.InspectionPending) || await _inspections.FindLatestAsync(parcel.Id, cancellationToken) is not null;

    public async Task<(string Why, string? Reason)?> ChooseAsync(OperationsParcel parcel, bool firstCheck, string? flagReason, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        if (parcel.Flags.HasFlag(OperationsParcelFlags.InspectionPending) || await _inspections.FindLatestAsync(parcel.Id, cancellationToken) is not null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(flagReason))
        {
            return ("flagged", flagReason);
        }

        if (await RuleAsync(parcel, null, cancellationToken) is { } rule)
        {
            return ("rule", rule);
        }

        int percent = (await _settings.FindAsync(cancellationToken))?.InspectionPercent ?? OperationsTenantSettings.DefaultInspectionPercent;
        return firstCheck && _draw.Selects(percent) ? ("random", null) : null;
    }

    // After the customer declares the package or gives its shipment a destination: the rules may now match (design section 1a).
    public async Task RecheckAsync(OperationsParcel parcel, Vixly.Operations.Domain.Shipments.OperationsShipment? shipment, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        // Only a parcel already checked at the station and still at its warehouse; the station checks the others itself.
        if (!parcel.IsAtOrigin || parcel.Flags.HasFlag(OperationsParcelFlags.AwaitingCheck) || parcel.Flags.HasFlag(OperationsParcelFlags.InspectionPending)
            || await _inspections.FindLatestAsync(parcel.Id, cancellationToken) is not null
            || await RuleAsync(parcel, shipment, cancellationToken) is not { } rule)
        {
            return;
        }

        await OpenAsync(parcel, ("rule", rule), null, "rck:" + parcel.Id.ToString("N") + ":" + utcNow.UtcTicks, OperationsTrackingActor.System, OperationsTrackingSource.System, null, utcNow, cancellationToken);
    }

    // The inspection record and the "inspection required" event, in the caller's unit of work.
    public async Task<OperationsInspection?> OpenAsync(OperationsParcel parcel, (string Why, string? Reason) choice, Guid? userId, string key, OperationsTrackingActor actor,
        OperationsTrackingSource source, string? locationCode, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        OperationsInspection inspection = OperationsInspection.Select(parcel.TenantId, parcel, choice.Why, choice.Reason, userId, utcNow);
        OperationsTrackingResult result = await _tracker.RecordAsync(new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.InspectionRequired,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["why"] = choice.Why }, utcNow, key, actor, source)
        {
            ActorUserId = userId,
            LocationCode = locationCode,
            Note = choice.Why == "flagged" ? inspection.Reason : null
        }, cancellationToken);

        if (result.Error is not null)
        {
            return null;
        }

        if (result.Created)
        {
            _inspections.Add(inspection);
        }

        return inspection;
    }

    // The shipment just created by the customer is not in the database yet: the caller passes it (review of 3C).
    public async Task<OperationsInspectionSubject> SubjectAsync(OperationsParcel parcel, CancellationToken cancellationToken, Vixly.Operations.Domain.Shipments.OperationsShipment? shipment = null)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        string? country = shipment is not null && shipment.Id == parcel.ShipmentId
            ? shipment.DestinationCountryCode
            : parcel.ShipmentId is { } shipmentId ? (await _receiving.GetShipmentAsync(shipmentId, cancellationToken))?.DestinationCountryCode : null;
        return new OperationsInspectionSubject(parcel.DeclaredContents, parcel.DeclaredValue, parcel.Carrier, country);
    }

    private async Task<string?> RuleAsync(OperationsParcel parcel, Vixly.Operations.Domain.Shipments.OperationsShipment? shipment, CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationsInspectionRule> rules = await _inspections.ListRulesAsync(cancellationToken);

        if (!rules.Any(rule => rule.IsActive))
        {
            return null;
        }

        IReadOnlyList<OperationsRestrictedItem> items = rules.Any(rule => rule.IsActive && rule.Kind == OperationsInspectionRuleKind.ListedItems)
            ? await _inspections.ListItemsAsync(cancellationToken)
            : Array.Empty<OperationsRestrictedItem>();
        return OperationsInspectionSelector.FirstMatch(await SubjectAsync(parcel, cancellationToken, shipment), rules, items);
    }
}
