using Vixly.Operations.Application.Reports;
using Vixly.Operations.Domain.Reports;

namespace Vixly.Operations.Application.UseCases.Reports.SaveOperationsReportSchedule;

// =========================================================
// FILE: SaveOperationsReportScheduleRequest.cs
//
// PURPOSE:
// Sets a report to be emailed on a schedule to staff of the company (Slice 11; owner, 2026-10-22).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsReportScheduleRequest
{
    public string? Key { get; init; }

    public OperationsReportFrequency Frequency { get; init; }

    public OperationsReportFormat Format { get; init; }

    public Guid? WarehouseId { get; init; }

    public OperationsReportGrain Grain { get; init; } = OperationsReportGrain.Day;

    public OperationsReportDimension? Dimension { get; init; }

    public int Days { get; init; } = 90;

    public IReadOnlyCollection<Guid> Recipients { get; init; } = Array.Empty<Guid>();
}
