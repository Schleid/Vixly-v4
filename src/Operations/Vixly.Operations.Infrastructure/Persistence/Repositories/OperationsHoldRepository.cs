using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsHoldRepository.cs
//
// PURPOSE:
// Reads and writes hold answers and finds waiting packages (Slice 3E).
//
// RESPONSIBILITIES:
// - Implement IOperationsHoldRepository over the Operations database
//
// IMPORTANT NOTES:
// - A package waits when it is still at its warehouse with a hold its customer must answer, or with no customer at all
// - Only the tenant list ignores the tenant filter, and only in a system scope
// =========================================================
internal sealed class OperationsHoldRepository : IOperationsHoldRepository
{
    // The holds that wait for the customer, and an unidentified package (no customer to wait for).
    private const OperationsHoldReasons Waiting = OperationsHoldReasons.AwaitingCustomerInformation | OperationsHoldReasons.Damaged | OperationsHoldReasons.InspectionDocuments
        | OperationsHoldReasons.InspectionPermit | OperationsHoldReasons.InspectionValue | OperationsHoldReasons.InspectionFee | OperationsHoldReasons.Unidentified
        | OperationsHoldReasons.AwaitingShipment;

    private readonly OperationsDbContext _context;

    public OperationsHoldRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsHoldAnswer answer) => _context.HoldAnswers.Add(answer ?? throw new ArgumentNullException(nameof(answer)));

    public Task<OperationsHoldAnswer?> GetAnswerAsync(Guid answerId, CancellationToken cancellationToken) =>
        _context.HoldAnswers.FirstOrDefaultAsync(item => item.Id == answerId, cancellationToken);

    public Task<OperationsHoldAnswer?> FindOpenAnswerAsync(Guid parcelId, OperationsHoldReasons reason, CancellationToken cancellationToken) =>
        _context.HoldAnswers.FirstOrDefaultAsync(item => item.ParcelId == parcelId && item.Reason == reason && item.Status == OperationsHoldAnswerStatus.Open, cancellationToken);

    public Task<bool> HasOpenAnswerAsync(Guid parcelId, CancellationToken cancellationToken) =>
        _context.HoldAnswers.AnyAsync(item => item.ParcelId == parcelId && item.Status == OperationsHoldAnswerStatus.Open, cancellationToken);

    public async Task<IReadOnlyList<OperationsHoldAnswer>> ListLatestAnswersAsync(IReadOnlyCollection<Guid> parcelIds, CancellationToken cancellationToken)
    {
        List<Guid> ids = parcelIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return Array.Empty<OperationsHoldAnswer>();
        }

        List<OperationsHoldAnswer> all = await _context.HoldAnswers.AsNoTracking()
            .Where(item => ids.Contains(item.ParcelId) && item.Status != OperationsHoldAnswerStatus.Replaced).ToListAsync(cancellationToken);
        return all.GroupBy(item => (item.ParcelId, item.Reason)).Select(group => group.OrderByDescending(item => item.CreatedUtc).First()).ToList();
    }

    public async Task<IReadOnlyList<OperationsParcel>> ListWaitingAsync(DateTimeOffset receivedBefore, int take, CancellationToken cancellationToken) =>
        await WaitingSince(receivedBefore).Where(item => item.CustomerId != null && item.RemindersSent < OperationsHoldLimits.ReminderDays.Count).OrderBy(item => item.CreatedUtc).Take(take).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<OperationsParcel> Items, int TotalCount)> ListUnclaimedAsync(IReadOnlyCollection<Guid> warehouseIds, DateTimeOffset receivedBefore, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        List<Guid> warehouses = warehouseIds.ToList();

        if (warehouses.Count == 0)
        {
            return (Array.Empty<OperationsParcel>(), 0);
        }

        IQueryable<OperationsParcel> query = WaitingSince(receivedBefore).AsNoTracking().Where(item => warehouses.Contains(item.WarehouseId!.Value));
        int total = await query.CountAsync(cancellationToken);
        List<OperationsParcel> items = await query.OrderBy(item => item.CreatedUtc).ThenBy(item => item.Id).Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<Guid>> ListTenantsWithWaitingAsync(DateTimeOffset receivedBefore, CancellationToken cancellationToken) =>
        await WaitingSince(receivedBefore).AsNoTracking().Select(item => item.TenantId).Distinct().Take(10_000).ToListAsync(cancellationToken);

    // Still at the warehouse (no milestone after receipt), with a hold for its customer, received before the date, and no answer
    // of the customer waiting for staff (a package whose customer answered is neither reminded nor unclaimed).
    private IQueryable<OperationsParcel> WaitingSince(DateTimeOffset receivedBefore) =>
        _context.Parcels.Where(item => item.WarehouseId != null && item.CreatedUtc < receivedBefore
            && (item.Milestone == null || item.Milestone == OperationsMilestone.ReceivedAtOrigin)
            && (item.HoldReasons & Waiting) != OperationsHoldReasons.None
            && !_context.HoldAnswers.Any(answer => answer.ParcelId == item.Id && answer.Status == OperationsHoldAnswerStatus.Open));
}
