using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentBooking.cs
//
// PURPOSE:
// Makes a shipment final ("Booked") when all its parcels are in (Slice 5C, design C-2; owner, 2026-10-20).
//
// RESPONSIBILITIES:
// - Say how far a shipment is from final: parcels in against the count, and what still stops it
// - Price it for its service with the company's prices, as the calculator and quotes do
// - Book it: fix the amount, lock it, and write the booked event on each of its parcels
// - Slice 5E: sell the supplies added to it (their stock moves)
// - Slice 5F: issue its invoice (I-1)
//
// IMPORTANT NOTES:
// - Called by whatever completes a shipment: a parcel entered at the counter, a parcel checked at the station, the customer changing the count
// - Final only when every parcel counted is measured, clear of holds and not waiting for an inspection, and no package announced in it is still on its way;
//   a dock delivery with more parcels than declared waits
//   for the customer to raise the count (C-4)
// - The caller saves; nothing here saves
// =========================================================
public sealed class OperationsShipmentBooking
{
    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsRater _rater;

    private readonly OperationsTracker _tracker;

    private readonly Vixly.Operations.Application.Supplies.OperationsSupplySales _supplies;

    private readonly IOperationsSupplyRepository _supplyRepository;

    private readonly Vixly.Operations.Application.Billing.OperationsInvoicing _invoicing;

    private readonly Vixly.Operations.Application.Settings.OperationsNumbering _numbering;

    public OperationsShipmentBooking(IOperationsReceivingRepository receiving, OperationsRater rater, OperationsTracker tracker,
        Vixly.Operations.Application.Supplies.OperationsSupplySales supplies, IOperationsSupplyRepository supplyRepository, Vixly.Operations.Application.Billing.OperationsInvoicing invoicing,
        Vixly.Operations.Application.Settings.OperationsNumbering numbering)
    {
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _supplyRepository = supplyRepository ?? throw new ArgumentNullException(nameof(supplyRepository));
        _invoicing = invoicing ?? throw new ArgumentNullException(nameof(invoicing));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _rater = rater ?? throw new ArgumentNullException(nameof(rater));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
    }

    // The parcels that travel in it: every one not cancelled, returned or disposed of.
    public static IReadOnlyList<OperationsParcel> Counted(IEnumerable<OperationsParcel> parcels) =>
        parcels.Where(parcel => parcel.Milestone is null or < OperationsMilestone.Cancelled).ToList();

    // Ready for its price and its commercial invoice: measured, described with a value, not waiting for a check or an inspection, no hold.
    public static bool Clear(OperationsParcel parcel) =>
        !string.IsNullOrWhiteSpace(parcel.DeclaredContents) && parcel.DeclaredValue is >= 0m
        && parcel.WeightKg is > 0m && parcel.LengthCm is > 0m && parcel.WidthCm is > 0m && parcel.HeightCm is > 0m
        && (parcel.Flags & (OperationsParcelFlags.AwaitingCheck | OperationsParcelFlags.InspectionPending | OperationsParcelFlags.InspectionInProgress)) == OperationsParcelFlags.None
        && parcel.HoldReasons == OperationsHoldReasons.None;

    // The price of its service for these parcels now, in the company's currency; no rating when the company has no price for it on this route.
    public async Task<(OperationsRating? Rating, string CurrencyCode)> PriceAsync(OperationsShipment shipment, IReadOnlyList<OperationsParcel> parcels, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(parcels);
        List<OperationsRatingPiece> pieces = parcels.Where(parcel => parcel.WeightKg is > 0m)
            .Select(parcel => new OperationsRatingPiece(parcel.WeightKg!.Value, parcel.LengthCm ?? 0m, parcel.WidthCm ?? 0m, parcel.HeightCm ?? 0m, parcel.DeclaredValue ?? 0m, null,
                parcel.DeclaredContents)).ToList();

        OperationsRatingSetup setup = await _rater.LoadAsync(shipment.TenantId, shipment.CustomerId, cancellationToken);

        if (pieces.Count == 0 || shipment.ServiceCode is not { } service)
        {
            return (null, setup.Settings.CurrencyCode);
        }

        OperationsWarehouse? origin = setup.Warehouses.FirstOrDefault(warehouse => warehouse.Id == shipment.OriginWarehouseId);
        OperationsDeliveryModes delivery = shipment.DeliveryAddress is null ? OperationsDeliveryModes.Pickup : OperationsDeliveryModes.HomeDelivery;
        return (setup.Rate(service, origin, setup.Destination(shipment.DestinationWarehouseId), shipment.CustomerId, pieces, shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate),
            shipment.Options.HasFlag(OperationsShipmentOptions.Insured), _rater.UtcNow, delivery, null, shipment.InsurancePlanId), setup.Settings.CurrencyCode);
    }

    // Slice 5D: what each insurance product would cost for these parcels (null: this value cannot be insured with it), and the total without insurance.
    public async Task<(decimal? BaseTotal, IReadOnlyDictionary<Guid, decimal?> Premiums)> InsuranceOptionsAsync(OperationsShipment shipment, IReadOnlyList<OperationsParcel> parcels,
        IReadOnlyList<Vixly.Operations.Domain.Charges.OperationsInsurancePlan> products, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(parcels);
        ArgumentNullException.ThrowIfNull(products);
        Dictionary<Guid, decimal?> premiums = new();
        List<OperationsRatingPiece> pieces = parcels.Where(parcel => parcel.WeightKg is > 0m)
            .Select(parcel => new OperationsRatingPiece(parcel.WeightKg!.Value, parcel.LengthCm ?? 0m, parcel.WidthCm ?? 0m, parcel.HeightCm ?? 0m, parcel.DeclaredValue ?? 0m, null,
                parcel.DeclaredContents)).ToList();

        if (pieces.Count == 0 || shipment.ServiceCode is not { } service)
        {
            return (null, premiums);
        }

        OperationsRatingSetup setup = await _rater.LoadAsync(shipment.TenantId, shipment.CustomerId, cancellationToken);
        OperationsWarehouse? origin = setup.Warehouses.FirstOrDefault(warehouse => warehouse.Id == shipment.OriginWarehouseId);
        OperationsWarehouse? destination = setup.Destination(shipment.DestinationWarehouseId);
        OperationsDeliveryModes delivery = shipment.DeliveryAddress is null ? OperationsDeliveryModes.Pickup : OperationsDeliveryModes.HomeDelivery;
        bool consolidated = shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate);
        decimal? baseTotal = setup.Rate(service, origin, destination, shipment.CustomerId, pieces, consolidated, false, _rater.UtcNow, delivery)?.Total;

        foreach (Vixly.Operations.Domain.Charges.OperationsInsurancePlan product in products)
        {
            OperationsRating? rating = setup.Rate(service, origin, destination, shipment.CustomerId, pieces, consolidated, true, _rater.UtcNow, delivery, null, product.Id);
            premiums[product.Id] = rating is { InsuranceRefused: false } ? rating.Lines.Where(line => line.Kind == Vixly.Operations.Domain.Charges.OperationsChargeLineKind.Insurance).Sum(line => line.Amount) : null;
        }

        return (baseTotal, premiums);
    }

    // Books the shipment when everything is in; true when it became final now. Who and where go on the booked events.
    public async Task<bool> TryBookAsync(OperationsShipment shipment, OperationsTrackingActor actor, Guid? userId, string? locationCode, OperationsTrackingSource source,
        DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        if (shipment.Status != OperationsShipmentStatus.Open || !shipment.HasInstructions || shipment.ServiceCode is null || shipment.ParcelCount is not { } count)
        {
            return false;
        }

        // A package the customer announced in it and not yet arrived: the shipment waits for it, whatever the count says.
        if ((await _receiving.ListShipmentPreAlertsForUpdateAsync(shipment.Id, cancellationToken)).Any(preAlert => preAlert.Status == Vixly.Operations.Domain.Receiving.OperationsPreAlertStatus.Open))
        {
            return false;
        }

        IReadOnlyList<OperationsParcel> parcels = Counted(await _receiving.ListShipmentParcelsForUpdateAsync(shipment.Id, cancellationToken));

        if (parcels.Count != count || !parcels.All(Clear) || await PriceAsync(shipment, parcels, cancellationToken) is not ({ } rating, string currency))
        {
            return false;
        }

        shipment.Book(rating.Total, currency, utcNow);

        // Its number before anything refers to it: the supplies' stock movements and the invoice (review of 5F).
        if (shipment.Number is null)
        {
            shipment.Numbered(await _numbering.NextAsync(Vixly.Operations.Domain.Settings.OperationsNumberKind.Shipment, cancellationToken));
        }

        // Slice 5E (S-15): the supplies added while it was processed are sold with it, from the counter's stock.
        await _supplies.ConfirmForShipmentAsync(shipment, userId, utcNow, cancellationToken);

        // Slice 5F (I-1): its invoice, with the lines it was just priced with and its supplies.
        await _invoicing.IssueForShipmentAsync(shipment, rating, currency, await _supplyRepository.FindShipmentSaleAsync(shipment.Id, cancellationToken), utcNow, cancellationToken);

        foreach (OperationsParcel parcel in parcels)
        {
            OperationsTrackingDraft draft = new(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.Booked, new Dictionary<string, string>(StringComparer.Ordinal),
                utcNow, "book:" + shipment.Id.ToString("N") + ":" + parcel.Id.ToString("N"), actor, source)
            {
                ActorUserId = userId,
                LocationCode = locationCode
            };

            // Never half booked: the caller's save is abandoned rather than keep a final shipment without its events.
            if ((await _tracker.RecordAsync(draft, cancellationToken)).Error is { } refused)
            {
                throw new InvalidOperationException("The booked event of parcel " + parcel.Id + " was refused: " + refused.Code);
            }
        }

        return true;
    }
}
