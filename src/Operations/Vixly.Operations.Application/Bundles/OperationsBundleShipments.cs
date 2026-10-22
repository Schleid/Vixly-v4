using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsBundleShipments.cs
//
// PURPOSE:
// Describes shipments beside a bundle (Slice 6A, D-3): their parcels in bundles and where the missing ones are.
//
// RESPONSIBILITIES:
// - Count each shipment's parcels, those in bundles and in this one
// - Name the shelf or the bundle of every other parcel
//
// IMPORTANT NOTES:
// - Read only; bounded by the shipments given
// =========================================================
public sealed class OperationsBundleShipments
{
    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsBundleRepository _bundles;

    public OperationsBundleShipments(IOperationsReceivingRepository receiving, IOperationsBundleRepository bundles)
    {
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
    }

    public async Task<IReadOnlyList<OperationsBundleShipmentLine>> DescribeAsync(IEnumerable<Guid> shipmentIds, Guid bundleId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipmentIds);
        List<OperationsBundleShipmentLine> lines = new();

        foreach (Guid shipmentId in shipmentIds.Distinct().Take(100))
        {
            if (await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is not { } shipment)
            {
                continue;
            }

            IReadOnlyList<OperationsParcel> parcels = OperationsShipmentBooking.Counted(await _receiving.ListShipmentParcelsAsync(shipmentId, cancellationToken));
            List<OperationsParcel> elsewhere = parcels.Where(parcel => parcel.BundleId != bundleId).ToList();
            IReadOnlyDictionary<Guid, string> shelves = (await _receiving.ListLocationsByIdAsync(elsewhere.Where(parcel => parcel.StorageLocationId is not null)
                .Select(parcel => parcel.StorageLocationId!.Value).Distinct().ToList(), cancellationToken)).ToDictionary(location => location.Id, location => location.Code);
            IReadOnlyDictionary<Guid, string> others = (await _bundles.ListBundlesByIdAsync(elsewhere.Where(parcel => parcel.BundleId is not null).Select(parcel => parcel.BundleId!.Value)
                .Distinct().ToList(), cancellationToken)).ToDictionary(bundle => bundle.Id, bundle => bundle.Number);

            lines.Add(new OperationsBundleShipmentLine(shipment.Id, shipment.Number, parcels.Count, parcels.Count(parcel => parcel.BundleId is not null), parcels.Count(parcel => parcel.BundleId == bundleId),
                elsewhere.Select(parcel => new OperationsBundleMissing(parcel.Id, parcel.Number,
                    parcel.StorageLocationId is { } shelf && shelves.TryGetValue(shelf, out string? code) ? code : null,
                    parcel.BundleId is { } other && others.TryGetValue(other, out string? number) ? number : null)).ToList()));
        }

        return lines;
    }
}
