namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportGrain.cs
//
// PURPOSE:
// How a report over time is cut: by day, week or month (Slice 11).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Keys of their texts in OperationsText
// =========================================================
public enum OperationsReportGrain
{
    Day = 1,

    Week = 2,

    Month = 3
}
