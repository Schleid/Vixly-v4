namespace Vixly.Operations.Domain.Reports;

// =========================================================
// FILE: OperationsReportFormat.cs
//
// PURPOSE:
// The file a scheduled export is sent as (Slice 11; owner, 2026-10-22: CSV and Excel).
//
// RESPONSIBILITIES:
// - Name each format
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// =========================================================
public enum OperationsReportFormat
{
    Csv = 1,

    Xlsx = 2
}
