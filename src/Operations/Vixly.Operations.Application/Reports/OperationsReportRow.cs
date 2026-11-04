namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportRow.cs
//
// PURPOSE:
// One row of a report (Slice 11).
//
// RESPONSIBILITIES:
// - Carry its label and one value per column
//
// IMPORTANT NOTES:
// - A value may be missing (nothing to measure)
// =========================================================
public sealed record OperationsReportRow(OperationsReportLabel Label, IReadOnlyList<decimal?> Values);
