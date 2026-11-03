using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Reports;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsReportScheduleRepository.cs
//
// PURPOSE:
// Stores the scheduled exports (Slice 11).
//
// RESPONSIBILITIES:
// - Add, list, find, remove; the due ones of every company
//
// IMPORTANT NOTES:
// - Tenant-filtered by the model (D-F1); the due list runs under the system scope
// =========================================================
internal sealed class OperationsReportScheduleRepository : IOperationsReportScheduleRepository
{
    private readonly OperationsDbContext _context;

    public OperationsReportScheduleRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsReportSchedule schedule) => _context.ReportSchedules.Add(schedule ?? throw new ArgumentNullException(nameof(schedule)));

    public void Remove(OperationsReportSchedule schedule) => _context.ReportSchedules.Remove(schedule ?? throw new ArgumentNullException(nameof(schedule)));

    public async Task<IReadOnlyList<OperationsReportSchedule>> ListAsync(CancellationToken cancellationToken) =>
        await _context.ReportSchedules.OrderBy(schedule => schedule.CreatedUtc).ToListAsync(cancellationToken);

    public Task<OperationsReportSchedule?> GetAsync(Guid scheduleId, CancellationToken cancellationToken) =>
        _context.ReportSchedules.FirstOrDefaultAsync(schedule => schedule.Id == scheduleId, cancellationToken);

    public async Task<bool> ClaimAsync(Guid scheduleId, DateTimeOffset utcNow, DateTimeOffset leaseUntil, CancellationToken cancellationToken) =>
        await _context.ReportSchedules.Where(schedule => schedule.Id == scheduleId && schedule.NextRunUtc <= utcNow)
            .ExecuteUpdateAsync(set => set.SetProperty(schedule => schedule.NextRunUtc, leaseUntil), cancellationToken) == 1;

    public async Task<IReadOnlyList<(Guid TenantId, Guid ScheduleId)>> DueAsync(DateTimeOffset utcNow, int take, CancellationToken cancellationToken) =>
        (await _context.ReportSchedules.Where(schedule => schedule.NextRunUtc <= utcNow).OrderBy(schedule => schedule.NextRunUtc).Take(Math.Clamp(take, 1, 500))
            .Select(schedule => new { schedule.TenantId, schedule.Id }).ToListAsync(cancellationToken)).Select(item => (item.TenantId, item.Id)).ToList();
}
