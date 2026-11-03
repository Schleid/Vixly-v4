using System.Globalization;

namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportPeriods.cs
//
// PURPOSE:
// The days of a report (Slice 11): checks, the period before, and the cut of a day.
//
// RESPONSIBILITIES:
// - Refuse a period longer than 13 months or backwards
// - The period of the same length just before (for the comparison)
// - The first day of the day, week (Monday) or month a date falls in
//
// IMPORTANT NOTES:
// - Local dates only
// =========================================================
public static class OperationsReportPeriods
{
    public const int MaxMonths = 13;

    public static bool Valid(DateOnly from, DateOnly to) => to >= from && to < from.AddMonths(MaxMonths);

    public static (DateOnly From, DateOnly To) Before(DateOnly from, DateOnly to)
    {
        int days = to.DayNumber - from.DayNumber + 1;
        return (from.AddDays(-days), from.AddDays(-1));
    }

    public static DateOnly Start(DateOnly date, OperationsReportGrain grain) => grain switch
    {
        OperationsReportGrain.Week => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
        OperationsReportGrain.Month => new DateOnly(date.Year, date.Month, 1),
        _ => date
    };

    // Every cut from the first to the last day, so a quiet day still shows a row.
    public static IReadOnlyList<DateOnly> Cuts(DateOnly from, DateOnly to, OperationsReportGrain grain)
    {
        List<DateOnly> cuts = new();

        for (DateOnly cut = Start(from, grain); cut <= to; cut = grain switch
        {
            OperationsReportGrain.Week => cut.AddDays(7),
            OperationsReportGrain.Month => cut.AddMonths(1),
            _ => cut.AddDays(1)
        })
        {
            cuts.Add(cut);
        }

        return cuts;
    }

    public static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
