namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportQuery.cs
//
// PURPOSE:
// One run of a report (Slice 11).
//
// RESPONSIBILITIES:
// - Carry the report, the days, the cut, the warehouses allowed and the company’s rules
//
// IMPORTANT NOTES:
// - Days are local dates; the reader turns them into each warehouse’s time zone
// =========================================================
public sealed record OperationsReportQuery(
    OperationsReportDefinition Report,
    DateOnly From,
    DateOnly To,
    OperationsReportGrain Grain,
    OperationsReportDimension Dimension,
    IReadOnlyList<OperationsReportPlace> Places,
    string CompanyTimeZone)
{
    // Whether the warehouses are all the company’s (company-wide figures are then complete).
    public bool AllPlaces { get; init; }

    public bool Money { get; init; }

    public int Days { get; init; } = 90;

    public int GraceHours { get; init; }

    public DateTimeOffset UtcNow { get; init; }
}
