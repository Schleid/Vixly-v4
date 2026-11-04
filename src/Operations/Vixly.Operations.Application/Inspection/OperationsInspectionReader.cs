using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Inspection;

// =========================================================
// FILE: OperationsInspectionReader.cs
//
// PURPOSE:
// Puts inspections and their parcels together for the queue and the holds page (Slice 3C).
//
// RESPONSIBILITIES:
// - Read the parcels of a page of inspections in one query
// - Attach each held parcel's latest inspection
//
// IMPORTANT NOTES:
// - Read-only; the callers check permissions
// =========================================================
public sealed class OperationsInspectionReader
{
    private readonly IOperationsInspectionRepository _inspections;

    private readonly OperationsParcelReader _parcels;

    private readonly IOperationsHoldRepository _holds;

    public OperationsInspectionReader(IOperationsInspectionRepository inspections, OperationsParcelReader parcels, IOperationsHoldRepository holds)
    {
        _holds = holds ?? throw new ArgumentNullException(nameof(holds));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _parcels = parcels ?? throw new ArgumentNullException(nameof(parcels));
    }

    public async Task<IReadOnlyList<OperationsInspectionQueueItem>> WithParcelsAsync(IReadOnlyList<OperationsInspection> inspections, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inspections);
        IReadOnlyList<OperationsParcel> parcels = await _inspections.ListParcelsAsync(inspections.Select(item => item.ParcelId).ToList(), cancellationToken);
        Dictionary<Guid, OperationsParcelSummary> summaries = (await _parcels.SummariesAsync(parcels, cancellationToken)).ToDictionary(item => item.ParcelId);
        return inspections.Where(item => summaries.ContainsKey(item.ParcelId)).Select(item => new OperationsInspectionQueueItem(item, summaries[item.ParcelId])).ToList();
    }

    public async Task<IReadOnlyList<OperationsHeldParcel>> HeldAsync(IReadOnlyList<OperationsParcel> parcels, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcels);
        List<Guid> ids = parcels.Select(item => item.Id).ToList();
        Dictionary<Guid, OperationsInspection> latest = (await _inspections.ListLatestAsync(ids, cancellationToken)).ToDictionary(item => item.ParcelId);
        ILookup<Guid, Vixly.Operations.Domain.Holds.OperationsHoldAnswer> answers = (await _holds.ListLatestAnswersAsync(ids, cancellationToken)).ToLookup(item => item.ParcelId);
        return (await _parcels.SummariesAsync(parcels, cancellationToken))
            .Select(item => new OperationsHeldParcel(item, latest.GetValueOrDefault(item.ParcelId)) { Answers = answers[item.ParcelId].ToList() }).ToList();
    }
}
