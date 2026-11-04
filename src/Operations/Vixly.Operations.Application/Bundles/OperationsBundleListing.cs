using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsBundleListing.cs
//
// PURPOSE:
// A bundle's list (owner, 2026-10-21): what may go on it from the computer, and its lines entered, scanned and missing.
//
// RESPONSIBILITIES:
// - The parcels and closed repack boxes at the bundle's warehouse that it would take now and that are on no open bundle's list
// - The list's lines with their numbers
//
// IMPORTANT NOTES:
// - The same rules as a scan at the station: final, not held, its destination and service, free to leave the origin; a repacked shipment goes in its closed box
// - The scan checks everything again; a kept-together shipment is checked again when the bundle closes
// =========================================================
public sealed class OperationsBundleListing
{
    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsBillingRepository _billing;

    public OperationsBundleListing(IOperationsBundleRepository bundles, IOperationsBillingRepository billing)
    {
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    public async Task<IReadOnlyList<OperationsBundleCandidate>> CandidatesAsync(OperationsBundle bundle, OperationsTenantSettings settings, CancellationToken cancellationToken,
        bool ownList = false)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        ArgumentNullException.ThrowIfNull(settings);

        if (!bundle.IsOpen)
        {
            return Array.Empty<OperationsBundleCandidate>();
        }

        Guid[] here = { bundle.WarehouseId };
        // With its own list: what could still be scanned into it (a missing line not among them is blocked).
        IReadOnlyList<OperationsBundleItem> planned = (await _bundles.ListPlannedAsync(here, cancellationToken)).Where(item => !ownList || item.BundleId != bundle.Id).ToList();
        HashSet<Guid> plannedParcels = planned.Where(item => item.ParcelId is not null).Select(item => item.ParcelId!.Value).ToHashSet();
        HashSet<Guid> plannedBoxes = planned.Where(item => item.BoxId is not null).Select(item => item.BoxId!.Value).ToHashSet();
        List<(OperationsParcel Parcel, OperationsShipment Shipment)> ready = (await _bundles.ListReadyAsync(here, 3000, cancellationToken))
            .Where(item => bundle.Refuses(item.Shipment) is null && OperationsBundling.Ready(item.Parcel, item.Shipment) is null).ToList();
        HashSet<Guid> held = (await _billing.ListShipmentInvoicesAsync(ready.Select(item => item.Shipment.Id).Distinct().ToList(), cancellationToken))
            .Where(invoice => invoice.ShipmentId is not null).GroupBy(invoice => invoice.ShipmentId!.Value).Where(group => OperationsBundling.Unpaid(group)).Select(group => group.Key).ToHashSet();
        ready = ready.Where(item => !held.Contains(item.Shipment.Id) || item.Shipment.FreightCollect).ToList();
        List<OperationsBundleCandidate> found = ready
            .Where(item => item.Parcel.BoxId is null && !plannedParcels.Contains(item.Parcel.Id)
                && !(item.Shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate) && item.Shipment.WayOr(settings.ConsolidationWay) == OperationsConsolidationWay.Repack))
            .Select(item => new OperationsBundleCandidate(item.Parcel.Id, false, item.Parcel.Number ?? string.Empty, item.Shipment.Id, item.Shipment.Number, 1, item.Parcel.WeightKg ?? 0m))
            .ToList();
        ILookup<Guid, (OperationsParcel Parcel, OperationsShipment Shipment)> inBoxes = ready.Where(item => item.Parcel.BoxId is not null).ToLookup(item => item.Parcel.BoxId!.Value);

        foreach (OperationsRepackBox box in (await _bundles.ListBoxesAsync(here, 1000, cancellationToken))
            .Where(box => box.Status == OperationsRepackBoxStatus.Closed && box.BundleId is null && !plannedBoxes.Contains(box.Id) && inBoxes[box.Id].Any()))
        {
            OperationsShipment shipment = inBoxes[box.Id].First().Shipment;
            found.Add(new OperationsBundleCandidate(box.Id, true, box.Number, shipment.Id, shipment.Number, inBoxes[box.Id].Count(), box.WeightKg ?? 0m));
        }

        return found.OrderBy(item => item.ShipmentNumber, StringComparer.Ordinal).ThenBy(item => item.Number, StringComparer.Ordinal).ToList();
    }

    // takeable: what CandidatesAsync(..., ownList: true) already returned, when the caller has it.
    public async Task<IReadOnlyList<OperationsBundleListRow>> RowsAsync(OperationsBundle bundle, OperationsTenantSettings settings, IReadOnlyList<OperationsBundleItem> items,
        CancellationToken cancellationToken, IReadOnlyList<OperationsBundleCandidate>? takeable = null)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        ArgumentNullException.ThrowIfNull(items);
        IReadOnlyDictionary<Guid, (string Number, string? ShipmentNumber)> names = await _bundles.NameItemsAsync(items, cancellationToken);
        HashSet<Guid> scannable = items.Any(item => !item.IsScanned) && bundle.IsOpen
            ? (takeable ?? await CandidatesAsync(bundle, settings, cancellationToken, true)).Select(candidate => candidate.Id).ToHashSet()
            : new HashSet<Guid>();
        return items.Select(item => new OperationsBundleListRow(item.Id, item.BoxId is not null, names.GetValueOrDefault(item.Id).Number ?? string.Empty,
            names.GetValueOrDefault(item.Id).ShipmentNumber, item.IsScanned, item.ScannedUtc)
            {
                Blocked = !item.IsScanned && bundle.IsOpen && !scannable.Contains(item.ParcelId ?? item.BoxId!.Value)
            }).OrderBy(row => row.Scanned).ThenBy(row => row.ShipmentNumber, StringComparer.Ordinal)
            .ThenBy(row => row.Number, StringComparer.Ordinal).ToList();
    }
}
