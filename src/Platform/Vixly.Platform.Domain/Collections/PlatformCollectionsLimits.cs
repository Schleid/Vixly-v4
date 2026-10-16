namespace Vixly.Platform.Domain.Collections;

// =========================================================
// FILE: PlatformCollectionsLimits.cs
//
// PURPOSE:
// Fixed bounds for collections settings, notes and reasons, and the day arithmetic of the timeline.
//
// RESPONSIBILITIES:
// - Hold the bounds the settings are checked against
// - Count days the way Billing counts overdue days
//
// IMPORTANT NOTES:
// - Day 1 is the first day an invoice is overdue: the day after its due date, from 00:00 UTC (Slice 4, D-B8)
// =========================================================
public static class PlatformCollectionsLimits
{
    public const string SingletonKey = "default";

    public const int MaxBlockDay = 60;

    public const int MaxRetryDays = 5;

    public const int MaxNoticeDays = 30;

    public const int MaxBlockedReadOnlyDays = 90;

    public const int MaxExtensionDays = 60;

    public const int MaxExtensionsPerCase = 10;

    public const int MaxPendingPaymentReadOnlyDays = 60;

    public const int MinLongBlockDays = 7;

    public const int MaxLongBlockDays = 365;

    public const int NoteMaxLength = 1000;

    public const int ReasonMaxLength = 500;

    public const int RetryKeyMaxLength = 80;

    public const int BatchSize = 50;

    public static readonly TimeSpan ExtensionEndingNotice = TimeSpan.FromDays(2);

    // Days since the due date, counted on UTC calendar days: 1 on the first overdue day.
    public static int DayOf(DateTimeOffset dueUtc, DateTimeOffset utcNow)
    {
        return (utcNow.UtcDateTime.Date - dueUtc.UtcDateTime.Date).Days;
    }

    // 00:00 UTC of a given day of the timeline.
    public static DateTimeOffset StartOfDay(DateTimeOffset dueUtc, int day)
    {
        return new DateTimeOffset(dueUtc.UtcDateTime.Date.AddDays(day), TimeSpan.Zero);
    }
}
