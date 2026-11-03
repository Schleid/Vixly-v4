using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsOutsideHours.cs
//
// PURPOSE:
// When an outside account (an outsourced call centre) may work: until an end date, on some days, between two times (owner, 2026-10-29, S-5).
//
// RESPONSIBILITIES:
// - Check the end date, the days and the times
// - Say whether a moment, in the company's time zone, is inside them
//
// IMPORTANT NOTES:
// - Days are a mask: Monday is 1, Tuesday 2, Wednesday 4 ... Sunday 64; 127 is every day
// - Times are minutes after midnight; no times means the whole day; a shift never crosses midnight
// =========================================================
public sealed record OperationsOutsideHours
{
    public const int EveryDay = 127;

    public const int MinutesInADay = 24 * 60;

    private OperationsOutsideHours(DateOnly? endsOn, int days, int? fromMinute, int? toMinute)
    {
        (EndsOn, Days, FromMinute, ToMinute) = (endsOn, days, fromMinute, toMinute);
    }

    public DateOnly? EndsOn { get; }

    public int Days { get; }

    public int? FromMinute { get; }

    public int? ToMinute { get; }

    public static OperationsOutsideHours Create(DateOnly? endsOn, int days, int? fromMinute, int? toMinute)
    {
        if (days is < 1 or > EveryDay)
        {
            throw new DomainException(OperationsOutsideErrors.DaysRequired);
        }

        if (fromMinute is null != toMinute is null
            || (fromMinute is { } from && toMinute is { } to && (from < 0 || to > MinutesInADay || from >= to)))
        {
            throw new DomainException(OperationsOutsideErrors.HoursInvalid);
        }

        return new OperationsOutsideHours(endsOn, days, fromMinute, toMinute);
    }

    // Monday is bit 0, Sunday bit 6.
    public static int DayBit(DayOfWeek day) => 1 << (((int)day + 6) % 7);

    public bool Covers(DateTime local)
    {
        if (EndsOn is { } end && DateOnly.FromDateTime(local) > end)
        {
            return false;
        }

        if ((Days & DayBit(local.DayOfWeek)) == 0)
        {
            return false;
        }

        int minute = (local.Hour * 60) + local.Minute;
        return FromMinute is not { } from || ToMinute is not { } to || (minute >= from && minute < to);
    }
}
