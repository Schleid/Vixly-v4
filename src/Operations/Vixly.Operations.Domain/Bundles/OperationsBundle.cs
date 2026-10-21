using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Bundles;

// =========================================================
// FILE: OperationsBundle.cs
//
// PURPOSE:
// A bundle: what the company ships, the carrier's unit (a box, a sack, a pallet), for one destination and one service (Slice 6A, D-2; owner, 2026-10-20).
//
// RESPONSIBILITIES:
// - Take parcels and repack boxes in, against its size policy
// - Close it with its weight and size; void it with a reason
//
// IMPORTANT NOTES:
// - Its destination is a destination warehouse, or a country for home delivery with none
// - Never reopened once closed: a wrong one is voided and the work starts again (owner, A-4)
// - Its parcel count and weights are stored, so two scans at once clash on its row version
// =========================================================
public sealed class OperationsBundle : OperationsEntity
{
    private OperationsBundle()
    {
        (Number, DestinationCountryCode, ServiceCode) = (string.Empty, string.Empty, string.Empty);
    }

    private OperationsBundle(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        (Number, DestinationCountryCode, ServiceCode) = (string.Empty, string.Empty, string.Empty);
    }

    public string Number { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid? DestinationWarehouseId { get; private set; }

    public string DestinationCountryCode { get; private set; }

    public string ServiceCode { get; private set; }

    // One of the company's bundle types (owner, A-5), as it was named when the bundle was made.
    public string? TypeName { get; private set; }

    public OperationsBundleBasis Basis { get; private set; }

    public decimal? TargetKg { get; private set; }

    // The service's volumetric divisor (cm³ per kg) when the bundle was made; none: the service has none.
    public decimal? Divisor { get; private set; }

    public OperationsBundleStatus Status { get; private set; }

    public int ParcelCount { get; private set; }

    public decimal ParcelsWeightKg { get; private set; }

    public decimal ParcelsVolumetricKg { get; private set; }

    public decimal? WeightKg { get; private set; }

    public decimal? LengthCm { get; private set; }

    public decimal? WidthCm { get; private set; }

    public decimal? HeightCm { get; private set; }

    public decimal? VolumetricKg { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    // The ship manager's form, so a double click makes one bundle (ADR-0019).
    public Guid? RequestKey { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public Guid? ClosedByUserId { get; private set; }

    public DateTimeOffset? VoidedUtc { get; private set; }

    public Guid? VoidedByUserId { get; private set; }

    public string? VoidReason { get; private set; }

    public bool IsOpen => Status == OperationsBundleStatus.Open;

    // Slice 6B (D-3): the dispatch it is manifested on; it can no longer be voided.
    public Guid? DispatchId { get; private set; }

    // Slice 7 (R-1, R-11): where it is after it travelled (a hub or its destination), when it arrived, and when it was opened there.
    public Guid? CurrentWarehouseId { get; private set; }

    public DateTimeOffset? ArrivedUtc { get; private set; }

    public DateTimeOffset? OpenedUtc { get; private set; }

    public Guid HereWarehouseId => CurrentWarehouseId ?? WarehouseId;

    // Arrived at a warehouse off its dispatch: free for an onward dispatch from a hub, or to be opened at its destination.
    public void ArriveAt(Guid warehouseId, DateTimeOffset utcNow)
    {
        (CurrentWarehouseId, ArrivedUtc, DispatchId) = (warehouseId, utcNow, null);
        MarkAsModified(utcNow);
    }

    public void OpenAtDestination(DateTimeOffset utcNow)
    {
        OpenedUtc ??= utcNow;
        MarkAsModified(utcNow);
    }

    public void Manifest(Guid dispatchId, DateTimeOffset utcNow)
    {
        if (DispatchId is not null)
        {
            throw new DomainException(Vixly.Operations.Domain.Dispatch.OperationsDispatchErrors.BundleElsewhere);
        }

        if (Status != OperationsBundleStatus.Closed)
        {
            throw new DomainException(Vixly.Operations.Domain.Dispatch.OperationsDispatchErrors.BundleNotClosed);
        }

        DispatchId = dispatchId;
        MarkAsModified(utcNow);
    }

    public void Unmanifest(DateTimeOffset utcNow)
    {
        DispatchId = null;
        MarkAsModified(utcNow);
    }

    // How full it is, as its basis counts.
    public decimal Filled => Basis == OperationsBundleBasis.Volumetric ? ParcelsVolumetricKg : ParcelsWeightKg;

    public static OperationsBundle Create(Guid tenantId, OperationsBundleDraft draft, string number, Guid userId, Guid? requestKey, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        if (draft.WarehouseId == Guid.Empty || string.IsNullOrWhiteSpace(draft.CountryCode) || draft.DestinationWarehouseId == Guid.Empty)
        {
            throw new DomainException(OperationsBundleErrors.DestinationRequired);
        }

        string country;

        try
        {
            country = OperationsCountryCodes.Normalize(draft.CountryCode);
        }
        catch (DomainException)
        {
            throw new DomainException(OperationsBundleErrors.DestinationRequired);
        }

        if (string.IsNullOrWhiteSpace(draft.ServiceCode) || draft.ServiceCode.Trim().Length > 50)
        {
            throw new DomainException(OperationsBundleErrors.ServiceRequired);
        }

        if (!Enum.IsDefined(draft.Basis) || (draft.Basis == OperationsBundleBasis.Free) != (draft.TargetKg is null)
            || draft.TargetKg is <= 0m or > OperationsBundleLimits.MaxWeightKg)
        {
            throw new DomainException(OperationsBundleErrors.TargetInvalid);
        }

        if (draft.Basis == OperationsBundleBasis.Volumetric && draft.Divisor is not > 0m)
        {
            throw new DomainException(OperationsBundleErrors.DivisorRequired);
        }

        return new OperationsBundle(tenantId, utcNow)
        {
            Number = number,
            WarehouseId = draft.WarehouseId,
            DestinationWarehouseId = draft.DestinationWarehouseId,
            DestinationCountryCode = country,
            ServiceCode = draft.ServiceCode.Trim(),
            TypeName = TypeText(draft.TypeName),
            Basis = draft.Basis,
            TargetKg = draft.TargetKg is { } target ? decimal.Round(target, 3) : null,
            Divisor = draft.Divisor is > 0m ? draft.Divisor : null,
            Status = OperationsBundleStatus.Open,
            CreatedByUserId = userId,
            RequestKey = requestKey
        };
    }

    // A bundle type: the company's own word, short and plain (owner, A-5).
    public static string? TypeText(string? value)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return text is null || (text.Length <= OperationsBundleLimits.TypeMaxLength && !text.Any(char.IsControl) && text.IndexOfAny(['<', '>']) < 0)
            ? text
            : throw new DomainException(OperationsBundleErrors.TypeInvalid);
    }

    // Where the shipment goes, as bundles group it: its destination warehouse, or its country for home delivery with none.
    public static (Guid? WarehouseId, string? CountryCode) DestinationOf(OperationsShipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        return (shipment.DestinationWarehouseId, shipment.DestinationWarehouseId is null ? shipment.DestinationCountryCode : null);
    }

    // Why this shipment cannot travel in it (D-3): the destination first, then the service; none when it fits.
    public Vixly.Shared.Kernel.Errors.Error? Refuses(OperationsShipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        bool destination = DestinationWarehouseId is { } warehouse
            ? shipment.DestinationWarehouseId == warehouse
            : shipment.DestinationWarehouseId is null && shipment.DestinationCountryCode == DestinationCountryCode;

        return !destination ? OperationsBundleErrors.WrongDestination : shipment.ServiceCode != ServiceCode ? OperationsBundleErrors.WrongService : null;
    }

    // A piece's volumetric weight with the service's divisor; none without a divisor.
    public decimal VolumetricOf(decimal lengthCm, decimal widthCm, decimal heightCm) =>
        Divisor is { } divisor ? decimal.Round(lengthCm * widthCm * heightCm / divisor, 3) : 0m;

    // Takes parcels in (one parcel, or a repack box with its parcels); true when it is now past its target and the company warns (D-5).
    public bool Take(int parcels, decimal weightKg, decimal volumetricKg, OperationsBundleTargetRule rule, DateTimeOffset utcNow)
    {
        EnsureOpen();

        if (parcels < 1 || weightKg < 0m || volumetricKg < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(parcels));
        }

        if (ParcelCount + parcels > OperationsBundleLimits.MaxParcels)
        {
            throw new DomainException(OperationsBundleErrors.Full);
        }

        decimal after = Basis == OperationsBundleBasis.Volumetric ? ParcelsVolumetricKg + volumetricKg : ParcelsWeightKg + weightKg;
        bool past = TargetKg is { } target && after > target;

        if (past && rule == OperationsBundleTargetRule.Refuse)
        {
            throw new DomainException(OperationsBundleErrors.TargetReached);
        }

        (ParcelCount, ParcelsWeightKg, ParcelsVolumetricKg) = (ParcelCount + parcels, ParcelsWeightKg + weightKg, ParcelsVolumetricKg + volumetricKg);
        MarkAsModified(utcNow);
        return past;
    }

    // A wrong scan taken out of the open bundle (D-6).
    public void Release(int parcels, decimal weightKg, decimal volumetricKg, DateTimeOffset utcNow)
    {
        EnsureOpen();
        (ParcelCount, ParcelsWeightKg, ParcelsVolumetricKg) = (Math.Max(0, ParcelCount - parcels), Math.Max(0m, ParcelsWeightKg - weightKg), Math.Max(0m, ParcelsVolumetricKg - volumetricKg));
        MarkAsModified(utcNow);
    }

    // Weighed and measured (D-7); takes nothing more and is never reopened.
    public void Close(decimal weightKg, decimal lengthCm, decimal widthCm, decimal heightCm, Guid userId, DateTimeOffset utcNow)
    {
        EnsureOpen();

        if (ParcelCount == 0)
        {
            throw new DomainException(OperationsBundleErrors.Empty);
        }

        if (weightKg is <= 0m or > OperationsBundleLimits.MaxWeightKg || new[] { lengthCm, widthCm, heightCm }.Any(side => side is <= 0m or > OperationsBundleLimits.MaxSideCm))
        {
            throw new DomainException(OperationsBundleErrors.MeasurementsInvalid);
        }

        (WeightKg, LengthCm, WidthCm, HeightCm) = (decimal.Round(weightKg, 3), decimal.Round(lengthCm, 1), decimal.Round(widthCm, 1), decimal.Round(heightCm, 1));
        VolumetricKg = Divisor is null ? null : VolumetricOf(LengthCm.Value, WidthCm.Value, HeightCm.Value);
        (Status, ClosedUtc, ClosedByUserId) = (OperationsBundleStatus.Closed, utcNow, userId);
        MarkAsModified(utcNow);
    }

    // An empty open bundle, or a closed one until it is on a manifest (D-8, owner A-4); its parcels go back to ready to bundle.
    public void Void(string? reason, Guid userId, DateTimeOffset utcNow)
    {
        string? why = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        if (why is null || why.Length > OperationsBundleLimits.ReasonMaxLength || why.Any(char.IsControl))
        {
            throw new DomainException(OperationsBundleErrors.ReasonRequired);
        }

        if (Status == OperationsBundleStatus.Voided || (Status == OperationsBundleStatus.Open && ParcelCount > 0) || DispatchId is not null || ArrivedUtc is not null)
        {
            throw new DomainException(OperationsBundleErrors.CannotVoid);
        }

        (Status, VoidedUtc, VoidedByUserId, VoidReason) = (OperationsBundleStatus.Voided, utcNow, userId, why);
        MarkAsModified(utcNow);
    }

    private void EnsureOpen()
    {
        if (Status != OperationsBundleStatus.Open)
        {
            throw new DomainException(OperationsBundleErrors.NotOpen);
        }
    }
}
