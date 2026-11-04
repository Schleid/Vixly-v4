namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportColumn.cs
//
// PURPOSE:
// One column of a report (Slice 11).
//
// RESPONSIBILITIES:
// - Name it (its text key) and say how it is shown
//
// IMPORTANT NOTES:
// - Money columns are shown only to whoever holds the finance right
// =========================================================
public sealed record OperationsReportColumn(string Key, OperationsReportValue Value)
{
    public bool IsMoney => Value == OperationsReportValue.Money;
}
