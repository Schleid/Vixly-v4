namespace Vixly.Operations.Domain.Reports;

// =========================================================
// FILE: OperationsReportFrequency.cs
//
// PURPOSE:
// How often a scheduled export is sent (Slice 11).
//
// RESPONSIBILITIES:
// - Name each frequency
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// =========================================================
public enum OperationsReportFrequency
{
    // Yesterday, every morning.
    Daily = 1,

    // Last week (Monday to Sunday), every Monday.
    Weekly = 2,

    // Last month, on the first.
    Monthly = 3
}
