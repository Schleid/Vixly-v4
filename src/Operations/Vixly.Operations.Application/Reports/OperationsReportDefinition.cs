namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportDefinition.cs
//
// PURPOSE:
// What a report is (Slice 11): its key, group, columns and what it can be cut by.
//
// RESPONSIBILITIES:
// - Carry the facts the pages and the reader share
//
// IMPORTANT NOTES:
// - Finance: the finance right is needed for the whole report
// - CompanyWide: not cut by warehouse, so the right must cover the whole company
// =========================================================
public sealed record OperationsReportDefinition(
    string Key,
    OperationsReportGroup Group,
    IReadOnlyList<OperationsReportColumn> Columns,
    IReadOnlyList<OperationsReportDimension> Dimensions,
    int ChartColumn = 0)
{
    public bool Finance { get; init; }

    public bool CompanyWide { get; init; }

    // A picture of today (stock, ageing) as of the end date, not a period.
    public bool AsOf { get; init; }

    public bool UsesDays { get; init; }

    // A picture of right now (stock on hand): no date to choose.
    public bool Now { get; init; }

    public bool OverTime => Dimensions.Count > 0 && Dimensions[0] == OperationsReportDimension.Period;
}
