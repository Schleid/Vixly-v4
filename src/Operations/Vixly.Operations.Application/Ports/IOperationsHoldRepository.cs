using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsHoldRepository.cs
//
// PURPOSE:
// Stores customers' answers to holds and finds packages waiting for their customer (Slice 3E).
//
// RESPONSIBILITIES:
// - Add and read hold answers
// - Find the packages due a reminder and those past the company's unclaimed days
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1), except the list of companies with waiting packages, read by the reminder job in a system scope
// =========================================================
public interface IOperationsHoldRepository
{
    void Add(OperationsHoldAnswer answer);

    Task<OperationsHoldAnswer?> GetAnswerAsync(Guid answerId, CancellationToken cancellationToken);

    Task<OperationsHoldAnswer?> FindOpenAnswerAsync(Guid parcelId, OperationsHoldReasons reason, CancellationToken cancellationToken);

    // The customer answered and staff have not reviewed it yet.
    Task<bool> HasOpenAnswerAsync(Guid parcelId, CancellationToken cancellationToken);

    // The latest answer to each hold of the parcels, open or reviewed.
    Task<IReadOnlyList<OperationsHoldAnswer>> ListLatestAnswersAsync(IReadOnlyCollection<Guid> parcelIds, CancellationToken cancellationToken);

    // Packages at a warehouse waiting for their customer (or unidentified) since before the date, with no answer waiting for staff;
    // tracked, for the reminder job, and only those with a reminder still to send.
    Task<IReadOnlyList<OperationsParcel>> ListWaitingAsync(DateTimeOffset receivedBefore, int take, CancellationToken cancellationToken);

    Task<(IReadOnlyList<OperationsParcel> Items, int TotalCount)> ListUnclaimedAsync(IReadOnlyCollection<Guid> warehouseIds, DateTimeOffset receivedBefore, int page, int pageSize,
        CancellationToken cancellationToken);

    // System scope: the companies with a package waiting since before the date.
    Task<IReadOnlyList<Guid>> ListTenantsWithWaitingAsync(DateTimeOffset receivedBefore, CancellationToken cancellationToken);
}
