using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Vixly.Shared.Hosting.Maintenance;

// =========================================================
// FILE: VixlyMaintenanceBanner.cs
//
// PURPOSE:
// The notice a site shows before a maintenance window (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Word the notice of the next announced window of a site, with its message
//
// IMPORTANT NOTES:
// - The plane gives its own text ui.maintenance.banner with {start} and {end}; tenant planes never say "Vixly"
// - Times are UTC, written in the culture of the visitor
// =========================================================
public static class VixlyMaintenanceBanner
{
    public const string TextKey = "ui.maintenance.banner";

    public static string? Banner(this VixlyMaintenanceMonitor monitor, string site, IStringLocalizer text)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(text);

        if (monitor.Announced(site) is not { } window)
        {
            return null;
        }

        string words = text[TextKey].Value
            .Replace("{start}", window.StartsUtc.UtcDateTime.ToString("g", CultureInfo.CurrentCulture), StringComparison.Ordinal)
            .Replace("{end}", window.EndsUtc.UtcDateTime.ToString("g", CultureInfo.CurrentCulture), StringComparison.Ordinal);

        return window.Message(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) is { } message ? words + " " + message : words;
    }
}
