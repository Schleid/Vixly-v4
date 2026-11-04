namespace Vixly.Operations.Application.UseCases.Reports.RemoveOperationsReportSchedule;

// =========================================================
// FILE: RemoveOperationsReportScheduleRequest.cs
//
// PURPOSE:
// Stops a scheduled export (Slice 11).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveOperationsReportScheduleRequest
{
    public Guid ScheduleId { get; init; }
}
