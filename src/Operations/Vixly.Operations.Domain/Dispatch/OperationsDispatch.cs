using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsDispatch.cs
//
// PURPOSE:
// A dispatch: closed bundles leaving one origin for one destination by one service, on one or more legs (Slice 6B, D-2 to D-5; V3's dispatch, owner 2026-10-21).
//
// RESPONSIBILITIES:
// - Take closed bundles while planned; close (manifest) it with its bundles and legs
// - Depart and arrive its legs in order, with the times they happened
// - Be cancelled while planned or closed, with a reason
//
// IMPORTANT NOTES:
// - Its bundle count is stored, so two people adding at once clash on its row version
// - The customer's events are written by the use cases from these steps (D-5)
// =========================================================
public sealed class OperationsDispatch : OperationsEntity
{
    public const int NotesMaxLength = 500;

    public const int MaxLegs = 6;

    public const int MaxBundles = 500;

    // A time typed for a leg is at most this old (a late entry keeps the real time, ADR-0021; a typo is refused).
    public const int MaxBackdateDays = 30;

    private readonly List<OperationsDispatchLeg> _legs = new();

    private OperationsDispatch()
    {
        (Number, DestinationCountryCode, ServiceCode) = (string.Empty, string.Empty, string.Empty);
    }

    private OperationsDispatch(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        (Number, DestinationCountryCode, ServiceCode) = (string.Empty, string.Empty, string.Empty);
    }

    public string Number { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid? DestinationWarehouseId { get; private set; }

    public string DestinationCountryCode { get; private set; }

    public string ServiceCode { get; private set; }

    public DateTimeOffset? CutoffUtc { get; private set; }

    public string? Notes { get; private set; }

    public OperationsDispatchStatus Status { get; private set; }

    public int BundleCount { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid? RequestKey { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public DateTimeOffset? DepartedUtc { get; private set; }

    public DateTimeOffset? ArrivedUtc { get; private set; }

    public DateTimeOffset? CancelledUtc { get; private set; }

    public string? CancelReason { get; private set; }

    public IReadOnlyList<OperationsDispatchLeg> Legs => _legs;

    // Slice 7 (R-11): an onward dispatch from a hub: its parcels are already in the destination country.
    public bool Onward { get; private set; }

    // Slice 7 (R-1): receiving at the destination finished (missing parcels listed).
    public DateTimeOffset? ReceivedUtc { get; private set; }

    // Slice 7 (review): the warehouse that receives it, fixed when it is planned (its destination, or the hub serving it);
    // for a dispatch to a country only, the first warehouse there that scans it.
    public Guid? ReceivingWarehouseId { get; private set; }

    public bool ReceivedAt(Guid warehouseId) => ReceivingWarehouseId == warehouseId;

    public void ClaimReceiving(Guid warehouseId, DateTimeOffset utcNow)
    {
        if (ReceivingWarehouseId is null)
        {
            ReceivingWarehouseId = warehouseId;
            MarkAsModified(utcNow);
        }
    }

    public bool CanBeReceived => Status is OperationsDispatchStatus.Departed or OperationsDispatchStatus.Arrived && ReceivedUtc is null;

    public void Received(DateTimeOffset utcNow)
    {
        if (!CanBeReceived)
        {
            throw new DomainException(OperationsDispatchErrors.NotClosed);
        }

        ReceivedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // The leg under way when the first bundle is scanned at the destination: the latest departed and not yet arrived (review: not only the last leg).
    public OperationsDispatchLeg? LegUnderWay() =>
        Status == OperationsDispatchStatus.Departed ? _legs.OrderBy(item => item.Sequence).LastOrDefault(item => item.Status == OperationsDispatchLegStatus.Departed) : null;

    // Step 2 (D-7): the carrier's cost worked out at departure, and the total the dispatcher keeps (corrected with a note).
    public decimal? CostComputed { get; private set; }

    public decimal? CostTotal { get; private set; }

    public string? CostNote { get; private set; }

    public string? CostCurrencyCode { get; private set; }

    // Step 2 (D-6): what the Shipper's Letter of Instruction says beyond the dispatch itself.
    public bool ChargesCollect { get; private set; }

    public string? ExportReference { get; private set; }

    public OperationsDispatchDocuments Documents { get; private set; }

    public string? HandlingInstructions { get; private set; }

    public const int CostNoteMaxLength = 300;

    public const int ExportReferenceMaxLength = 40;

    public const int InstructionsMaxLength = 500;

    // Worked out (or again): a correction stays until the dispatcher clears it.
    public void Costed(decimal? computed, string currencyCode, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        (CostComputed, CostCurrencyCode) = (computed, currencyCode);

        if (CostNote is null)
        {
            CostTotal = computed;
        }

        MarkAsModified(utcNow);
    }

    // The dispatcher's total, with why; none puts back the worked-out cost.
    public void CorrectCost(decimal? total, string? note, string currencyCode, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        if (Status is OperationsDispatchStatus.Planned or OperationsDispatchStatus.Cancelled)
        {
            throw new DomainException(OperationsDispatchErrors.NotClosed);
        }

        if (total is null)
        {
            (CostTotal, CostNote) = (CostComputed, null);
        }
        else
        {
            if (total is < 0m or > OperationsCarrierRate.MaxAmount * 100 || decimal.Round(total.Value, 2) != total.Value)
            {
                throw new DomainException(OperationsDispatchErrors.CostInvalid);
            }

            (CostTotal, CostNote) = (total, OperationsDispatchText.Required(note, CostNoteMaxLength, OperationsDispatchErrors.ReasonRequired));
            CostCurrencyCode ??= currencyCode;
        }

        MarkAsModified(utcNow);
    }

    // Until it arrives.
    public void ChangeInstructions(bool collect, string? exportReference, OperationsDispatchDocuments documents, string? instructions, DateTimeOffset utcNow)
    {
        if (Status is OperationsDispatchStatus.Arrived or OperationsDispatchStatus.Cancelled)
        {
            throw new DomainException(OperationsDispatchErrors.NotPlanned);
        }

        if ((documents & ~(OperationsDispatchDocuments)63) != 0)
        {
            throw new DomainException(OperationsDispatchErrors.TextInvalid);
        }

        (ChargesCollect, ExportReference, Documents, HandlingInstructions) = (collect, OperationsDispatchText.Optional(exportReference, ExportReferenceMaxLength), documents,
            OperationsDispatchText.Optional(instructions, InstructionsMaxLength));
        MarkAsModified(utcNow);
    }

    // The airports of the master air waybill: the first air leg's departure, the last air leg's arrival; the whole route without an air leg (review).
    public (string From, string To) AirRoute()
    {
        List<OperationsDispatchLeg> ordered = _legs.OrderBy(item => item.Sequence).ToList();
        List<OperationsDispatchLeg> air = ordered.Where(item => item.Kind == OperationsCarrierKind.Air).ToList();
        List<OperationsDispatchLeg> route = air.Count > 0 ? air : ordered;
        return (route.FirstOrDefault()?.From ?? string.Empty, route.LastOrDefault()?.To ?? string.Empty);
    }

    public bool TravelsByAir => _legs.Any(item => item.Kind == OperationsCarrierKind.Air);

    // The master air waybill: the first air leg's reference, with its airline's prefix when the number lacks it (A-4).
    public string? MasterWaybill(IReadOnlyDictionary<Guid, OperationsCarrier> carriers)
    {
        ArgumentNullException.ThrowIfNull(carriers);

        if (_legs.OrderBy(item => item.Sequence).FirstOrDefault(item => item.Kind == OperationsCarrierKind.Air && item.Reference is not null) is not { } leg)
        {
            return null;
        }

        string reference = leg.Reference!;
        return leg.CarrierId is { } id && carriers.GetValueOrDefault(id)?.AirlinePrefix is { } prefix && !reference.StartsWith(prefix, StringComparison.Ordinal)
            ? prefix + "-" + reference
            : reference;
    }

    public static OperationsDispatch Create(Guid tenantId, OperationsDispatchDraft draft, string number, Guid userId, Guid? requestKey, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        string country;

        try
        {
            country = OperationsCountryCodes.Normalize(draft.CountryCode);
        }
        catch (DomainException)
        {
            throw new DomainException(OperationsDispatchErrors.DestinationRequired);
        }

        if (draft.WarehouseId == Guid.Empty || draft.DestinationWarehouseId == Guid.Empty)
        {
            throw new DomainException(OperationsDispatchErrors.DestinationRequired);
        }

        return new OperationsDispatch(tenantId, utcNow)
        {
            Number = number,
            WarehouseId = draft.WarehouseId,
            DestinationWarehouseId = draft.DestinationWarehouseId,
            DestinationCountryCode = country,
            ServiceCode = OperationsDispatchText.Required(draft.ServiceCode, 40, OperationsDispatchErrors.ServiceRequired),
            CutoffUtc = draft.CutoffUtc,
            Notes = OperationsDispatchText.Optional(draft.Notes, NotesMaxLength),
            Status = OperationsDispatchStatus.Planned,
            CreatedByUserId = userId,
            RequestKey = requestKey,
            Onward = draft.Onward,
            ReceivingWarehouseId = draft.ReceivingWarehouseId
        };
    }

    // Why this bundle cannot travel in it: not closed, another warehouse, another destination or service; none when it fits (D-3).
    public Vixly.Shared.Kernel.Errors.Error? Refuses(OperationsBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        return bundle.Status != OperationsBundleStatus.Closed || bundle.OpenedUtc is not null ? OperationsDispatchErrors.BundleNotClosed
            : bundle.HereWarehouseId != WarehouseId ? OperationsDispatchErrors.WrongWarehouse
            : bundle.DestinationWarehouseId != DestinationWarehouseId || bundle.DestinationCountryCode != DestinationCountryCode ? OperationsDispatchErrors.WrongDestination
            : bundle.ServiceCode != ServiceCode ? OperationsDispatchErrors.WrongService
            : null;
    }

    public void TakeBundle(OperationsBundle bundle, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        EnsurePlanned();

        if (Refuses(bundle) is { } refused)
        {
            throw new DomainException(refused);
        }

        if (BundleCount >= MaxBundles)
        {
            throw new DomainException(OperationsDispatchErrors.TooManyBundles);
        }

        bundle.Manifest(Id, utcNow);
        BundleCount++;
        MarkAsModified(utcNow);
    }

    public void ReleaseBundle(OperationsBundle bundle, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        EnsurePlanned();

        if (bundle.DispatchId != Id)
        {
            throw new DomainException(OperationsDispatchErrors.BundleElsewhere);
        }

        bundle.Unmanifest(utcNow);
        BundleCount = Math.Max(0, BundleCount - 1);
        MarkAsModified(utcNow);
    }

    public OperationsDispatchLeg AddLeg(OperationsDispatchLegDraft draft, DateTimeOffset utcNow)
    {
        if (Status is not (OperationsDispatchStatus.Planned or OperationsDispatchStatus.Closed))
        {
            throw new DomainException(OperationsDispatchErrors.NotPlanned);
        }

        // The same form posted twice: the leg it made (ADR-0019).
        ArgumentNullException.ThrowIfNull(draft);

        if (draft.RequestKey is { } key && _legs.FirstOrDefault(item => item.RequestKey == key) is { } made)
        {
            return made;
        }

        if (_legs.Count >= MaxLegs)
        {
            throw new DomainException(OperationsDispatchErrors.TooManyLegs);
        }

        OperationsDispatchLeg leg = new(_legs.Count + 1, draft);
        _legs.Add(leg);
        MarkAsModified(utcNow);
        return leg;
    }

    // A leg not yet departed, while the dispatch has not left.
    public void RemoveLeg(Guid legId, DateTimeOffset utcNow)
    {
        if (Status is not (OperationsDispatchStatus.Planned or OperationsDispatchStatus.Closed))
        {
            throw new DomainException(OperationsDispatchErrors.NotPlanned);
        }

        OperationsDispatchLeg leg = Leg(legId);

        // A closed dispatch keeps at least one leg.
        if (Status == OperationsDispatchStatus.Closed && _legs.Count == 1)
        {
            throw new DomainException(OperationsDispatchErrors.NothingToClose);
        }

        _legs.Remove(leg);
        int sequence = 0;

        foreach (OperationsDispatchLeg remaining in _legs.OrderBy(item => item.Sequence).ToList())
        {
            remaining.Renumber(++sequence);
        }

        MarkAsModified(utcNow);
    }

    // Manifested: its bundles and legs are set; nothing more is added (D-2).
    public void Close(DateTimeOffset utcNow)
    {
        EnsurePlanned();

        if (BundleCount == 0 || _legs.Count == 0)
        {
            throw new DomainException(OperationsDispatchErrors.NothingToClose);
        }

        (Status, ClosedUtc) = (OperationsDispatchStatus.Closed, utcNow);
        MarkAsModified(utcNow);
    }

    // Legs leave in order, each after the previous one arrived; the first leaving departs the dispatch. True when it is the first.
    public bool DepartLeg(Guid legId, DateTimeOffset at, DateTimeOffset utcNow)
    {
        if (Status is not (OperationsDispatchStatus.Closed or OperationsDispatchStatus.Departed))
        {
            throw new DomainException(OperationsDispatchErrors.NotClosed);
        }

        OperationsDispatchLeg leg = Leg(legId);
        List<OperationsDispatchLeg> ordered = _legs.OrderBy(item => item.Sequence).ToList();
        int index = ordered.IndexOf(leg);

        if (leg.Status != OperationsDispatchLegStatus.Planned || (index > 0 && ordered[index - 1].Status != OperationsDispatchLegStatus.Arrived))
        {
            throw new DomainException(OperationsDispatchErrors.LegOrder);
        }

        if (at > utcNow.AddMinutes(5) || at < utcNow.AddDays(-MaxBackdateDays) || (index > 0 && ordered[index - 1].ArrivedUtc is { } previous && at < previous))
        {
            throw new DomainException(OperationsDispatchErrors.LegTimeInvalid);
        }

        bool first = Status == OperationsDispatchStatus.Closed;
        leg.Depart(at);

        if (first)
        {
            (Status, DepartedUtc) = (OperationsDispatchStatus.Departed, at);
        }

        MarkAsModified(utcNow);
        return first;
    }

    // True when it was the last leg: the dispatch has arrived.
    public bool ArriveLeg(Guid legId, DateTimeOffset at, DateTimeOffset utcNow)
    {
        OperationsDispatchLeg leg = Leg(legId);

        if (Status != OperationsDispatchStatus.Departed || leg.Status != OperationsDispatchLegStatus.Departed)
        {
            throw new DomainException(OperationsDispatchErrors.LegOrder);
        }

        if (at > utcNow.AddMinutes(5) || at < utcNow.AddDays(-MaxBackdateDays) || (leg.DepartedUtc is { } departed && at < departed))
        {
            throw new DomainException(OperationsDispatchErrors.LegTimeInvalid);
        }

        leg.Arrive(at);
        bool last = _legs.All(item => item.Status == OperationsDispatchLegStatus.Arrived);

        if (last)
        {
            (Status, ArrivedUtc) = (OperationsDispatchStatus.Arrived, at);
        }

        MarkAsModified(utcNow);
        return last;
    }

    public bool IsLastLeg(Guid legId) => _legs.OrderBy(item => item.Sequence).Last().Id == legId;

    public void Cancel(string? reason, DateTimeOffset utcNow)
    {
        if (Status is not (OperationsDispatchStatus.Planned or OperationsDispatchStatus.Closed))
        {
            throw new DomainException(OperationsDispatchErrors.CannotCancel);
        }

        (Status, CancelledUtc, CancelReason) = (OperationsDispatchStatus.Cancelled, utcNow,
            OperationsDispatchText.Required(reason, 300, OperationsDispatchErrors.ReasonRequired));
        BundleCount = 0;
        MarkAsModified(utcNow);
    }

    // Cancelled: its bundles are free again (the use case takes each out).
    public static void Free(OperationsBundle bundle, DateTimeOffset utcNow) => (bundle ?? throw new ArgumentNullException(nameof(bundle))).Unmanifest(utcNow);

    public OperationsDispatchLeg Leg(Guid legId) => _legs.FirstOrDefault(item => item.Id == legId) ?? throw new DomainException(OperationsDispatchErrors.LegNotFound);

    private void EnsurePlanned()
    {
        if (Status != OperationsDispatchStatus.Planned)
        {
            throw new DomainException(OperationsDispatchErrors.NotPlanned);
        }
    }
}
