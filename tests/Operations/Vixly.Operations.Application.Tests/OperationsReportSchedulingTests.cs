using Vixly.Operations.Application.Reports;
using Vixly.Operations.Domain.Reports;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsReportSchedulingTests.cs
//
// PURPOSE:
// Proves when scheduled reports go and what they cover (Slice 11).
//
// RESPONSIBILITIES:
// - 06:00 local the next day, Monday or first of the month; yesterday, last week, last month; 1 to 10 recipients
//
// IMPORTANT NOTES:
// - Pure; no database
// =========================================================
public sealed class OperationsReportSchedulingTests
{
    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    [Fact]
    public void NextRun_IsSixInTheMorning_InTheCompanysTimeZone()
    {
        DateTimeOffset thursdayEvening = new(2026, 10, 22, 23, 30, 0, TimeSpan.Zero);
        Assert.Equal(new DateTimeOffset(2026, 10, 23, 10, 0, 0, TimeSpan.Zero), OperationsReportScheduling.NextRun(OperationsReportFrequency.Daily, thursdayEvening, NewYork));

        // Before six in the morning: today's run (review).
        Assert.Equal(new DateTimeOffset(2026, 10, 22, 10, 0, 0, TimeSpan.Zero),
            OperationsReportScheduling.NextRun(OperationsReportFrequency.Daily, new DateTimeOffset(2026, 10, 22, 8, 0, 0, TimeSpan.Zero), NewYork));
        Assert.Equal(new DateTimeOffset(2026, 10, 26, 10, 0, 0, TimeSpan.Zero), OperationsReportScheduling.NextRun(OperationsReportFrequency.Weekly, thursdayEvening, NewYork));
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 11, 0, 0, TimeSpan.Zero), OperationsReportScheduling.NextRun(OperationsReportFrequency.Monthly, thursdayEvening, NewYork));
    }

    [Fact]
    public void Covered_IsYesterday_LastWeek_OrLastMonth()
    {
        DateOnly monday = new(2026, 10, 26);
        Assert.Equal((new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 25)), OperationsReportScheduling.Covered(OperationsReportFrequency.Daily, monday));
        Assert.Equal((new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 25)), OperationsReportScheduling.Covered(OperationsReportFrequency.Weekly, monday));
        Assert.Equal((new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)), OperationsReportScheduling.Covered(OperationsReportFrequency.Monthly, new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public void Schedule_GoesToOneToTenPeople_AndMovesOnOnceSent()
    {
        DateTimeOffset now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(OperationsReportErrors.RecipientsInvalid, Assert.Throws<DomainException>(() => OperationsReportSchedule.Create(Guid.NewGuid(), "sales", OperationsReportFrequency.Daily,
            OperationsReportFormat.Csv, null, 1, null, 90, Array.Empty<Guid>(), Guid.NewGuid(), now, now)).Error);
        Assert.Equal(OperationsReportErrors.RecipientsInvalid, Assert.Throws<DomainException>(() => OperationsReportSchedule.Create(Guid.NewGuid(), "sales", OperationsReportFrequency.Daily,
            OperationsReportFormat.Csv, null, 1, null, 90, Enumerable.Range(0, 11).Select(_ => Guid.NewGuid()).ToList(), Guid.NewGuid(), now, now)).Error);
        Guid person = Guid.NewGuid();
        OperationsReportSchedule schedule = OperationsReportSchedule.Create(Guid.NewGuid(), "sales", OperationsReportFrequency.Daily, OperationsReportFormat.Csv, null, 1, null, 90,
            new[] { person, person }, Guid.NewGuid(), now.AddHours(1), now);
        Assert.Single(schedule.Recipients);
        Assert.False(schedule.Due(now));
        Assert.True(schedule.Due(now.AddHours(2)));
        schedule.Sent(now.AddDays(1), now.AddHours(2));
        Assert.False(schedule.Due(now.AddHours(3)));

        // Nothing went out: 10 minutes, then doubling; after eight rounds the period is skipped.
        schedule.Failed(now.AddDays(2), now);
        Assert.Equal(now.AddMinutes(10), schedule.NextRunUtc);
        schedule.Failed(now.AddDays(2), now);
        Assert.Equal(now.AddMinutes(20), schedule.NextRunUtc);

        for (int round = 3; round <= OperationsReportSchedule.MaxFailures; round++)
        {
            schedule.Failed(now.AddDays(2), now);
        }

        Assert.Equal((now.AddDays(2), 0), (schedule.NextRunUtc, schedule.Failures));
    }
}
