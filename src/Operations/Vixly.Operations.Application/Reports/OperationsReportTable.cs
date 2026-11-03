namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportTable.cs
//
// PURPOSE:
// The figures of a report run (Slice 11).
//
// RESPONSIBILITIES:
// - Carry the rows and the totals line
//
// IMPORTANT NOTES:
// - Totals are worked out by the reader: counts and money add up; days, hours and percents are weighted
// =========================================================
public sealed record OperationsReportTable(IReadOnlyList<OperationsReportRow> Rows, IReadOnlyList<decimal?> Totals)
{
    // A report cut in more than 500 rows shows the first 500 (the totals still cover everything).
    public const int MaxRows = 500;

    public bool Truncated { get; init; }
}
