using System.Globalization;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Web.Components.Dispatch;

// =========================================================
// FILE: OperationsDispatchWords.cs
//
// PURPOSE:
// Small formatting shared by the dispatch pages (Slice 6B).
//
// RESPONSIBILITIES:
// - Show a moment where the company works, and fill a date-time field
//
// IMPORTANT NOTES:
// - Times are entered and shown in the company's time zone
// =========================================================
public static class OperationsDispatchWords
{
    public static string Time(DateTimeOffset? moment, string? timeZone) =>
        moment is { } value ? OperationsTimeZones.Local(value, timeZone).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : string.Empty;

    public static string Field(DateTimeOffset? moment, string? timeZone) =>
        moment is { } value ? OperationsTimeZones.Local(value, timeZone).ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture) : string.Empty;

    public static string Zone(string? timeZone) => timeZone ?? "UTC";
}
