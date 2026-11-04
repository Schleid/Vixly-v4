using Vixly.Operations.Domain.Reports;

namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportScheduling.cs
//
// PURPOSE:
// When a scheduled export goes and which days it covers (Slice 11).
//
// RESPONSIBILITIES:
// - The next run: 06:00 in the company’s time zone, the next day, the next Monday or the first of next month
// - The days covered: yesterday, last week (Monday to Sunday) or last month
//
// IMPORTANT NOTES:
// - Local dates; the run time is turned into UTC with the company’s time zone
// =========================================================
public static class OperationsReportScheduling
{
    public static readonly TimeOnly RunAt = new(6, 0);

    public static DateTimeOffset NextRun(OperationsReportFrequency frequency, DateTimeOffset afterUtc, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        DateOnly today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(afterUtc, zone).DateTime);

        // The first 06:00 of a matching day after the moment given (today's, when it is still to come).
        for (DateOnly day = today; ; day = day.AddDays(1))
        {
            bool matches = frequency switch
            {
                OperationsReportFrequency.Weekly => day.DayOfWeek == DayOfWeek.Monday,
                OperationsReportFrequency.Monthly => day.Day == 1,
                _ => true
            };
            DateTime local = day.ToDateTime(RunAt);
            DateTimeOffset run = new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();

            if (matches && run > afterUtc)
            {
                return run;
            }
        }
    }

    // The days a run on that local day covers.
    public static (DateOnly From, DateOnly To) Covered(OperationsReportFrequency frequency, DateOnly runDay) => frequency switch
    {
        OperationsReportFrequency.Weekly => (OperationsReportPeriods.Start(runDay, OperationsReportGrain.Week).AddDays(-7), OperationsReportPeriods.Start(runDay, OperationsReportGrain.Week).AddDays(-1)),
        OperationsReportFrequency.Monthly => (new DateOnly(runDay.Year, runDay.Month, 1).AddMonths(-1), new DateOnly(runDay.Year, runDay.Month, 1).AddDays(-1)),
        _ => (runDay.AddDays(-1), runDay.AddDays(-1))
    };
}
