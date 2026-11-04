using Vixly.Operations.Domain.Reports;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsReportScheduleRepository.cs
//
// PURPOSE:
// The company’s scheduled exports (Slice 11).
//
// RESPONSIBILITIES:
// - Add, list, find and remove schedules; list the due ones of every company (system scope)
//
// IMPORTANT NOTES:
// - Tenant-filtered except the due list
// =========================================================
public interface IOperationsReportScheduleRepository
{
    void Add(OperationsReportSchedule schedule);

    void Remove(OperationsReportSchedule schedule);

    Task<IReadOnlyList<OperationsReportSchedule>> ListAsync(CancellationToken cancellationToken);

    Task<OperationsReportSchedule?> GetAsync(Guid scheduleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<(Guid TenantId, Guid ScheduleId)>> DueAsync(DateTimeOffset utcNow, int take, CancellationToken cancellationToken);

    // Takes a due schedule for one host: its next run is pushed past the lease in one conditional update. False when another host took it.
    Task<bool> ClaimAsync(Guid scheduleId, DateTimeOffset utcNow, DateTimeOffset leaseUntil, CancellationToken cancellationToken);
}
