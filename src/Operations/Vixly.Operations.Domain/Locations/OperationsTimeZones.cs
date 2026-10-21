using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsTimeZones.cs
//
// PURPOSE:
// Checks an IANA time zone, such as America/Port-au-Prince (Slice 4B).
//
// RESPONSIBILITIES:
// - Accept a known IANA time zone or nothing
//
// IMPORTANT NOTES:
// - Windows and Linux both read IANA names (.NET with ICU)
// =========================================================
public static class OperationsTimeZones
{
    public const int MaxLength = 64;

    public static string? Optional(string? value)
    {
        string? id = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        return id is null || (id.Length <= MaxLength && (id == "UTC" || id.Contains('/', StringComparison.Ordinal)) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out _))
            ? id
            : throw new DomainException(OperationsDestinationErrors.TimeZoneInvalid);
    }

    // Slice 4D: the calendar day it is now where the company works.
    public static DateOnly Today(DateTimeOffset utcNow, string? timeZone)
    {
        TimeZoneInfo zone = timeZone is not null && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out TimeZoneInfo? found) ? found : TimeZoneInfo.Utc;
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, zone).DateTime);
    }

    // The start of a calendar day in a time zone, as a moment (prices start at midnight where the company works).
    public static DateTimeOffset StartOfDay(DateOnly day, string? timeZone)
    {
        TimeZoneInfo zone = timeZone is not null && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out TimeZoneInfo? found) ? found : TimeZoneInfo.Utc;
        DateTime local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }
}
