using Vixly.Operations.Application.Reports;

namespace Vixly.Operations.Application.UseCases.Reports.RunOperationsReport;

// =========================================================
// FILE: RunOperationsReportRequest.cs
//
// PURPOSE:
// Runs one report for its days, cut and warehouse (Slice 11).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RunOperationsReportRequest
{
    public string? Key { get; init; }

    // None: the last 30 days (a picture: today).
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public OperationsReportGrain Grain { get; init; } = OperationsReportGrain.Day;

    public OperationsReportDimension? Dimension { get; init; }

    // None: every warehouse this person may see.
    public Guid? WarehouseId { get; init; }

    public int Days { get; init; } = 90;
}
