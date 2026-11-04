using Microsoft.Extensions.Localization;
using Vixly.Public.Web.Resources;
using Vixly.Shared.Hosting.Maintenance;

namespace Vixly.Public.Web.Hosting;

// =========================================================
// FILE: PublicMaintenancePages.cs
//
// PURPOSE:
// Closes the public site during a maintenance window (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Say which requests the window closes
// - Give the maintenance page its words
//
// IMPORTANT NOTES:
// - After the language step, so the page speaks the language of the visitor
// =========================================================
public static class PublicMaintenancePages
{
    public const string Site = "public";

    public static string? SiteOf(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        return VixlyMaintenance.IsAlwaysOpen(http.Request.Path) ? null : Site;
    }

    public static VixlyMaintenancePageText Text(HttpContext http, VixlyMaintenanceWindow window)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(window);

        IStringLocalizer<PublicText> text = http.RequestServices.GetRequiredService<IStringLocalizer<PublicText>>();
        string language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        return new VixlyMaintenancePageText(language, text["ui.maintenance.page_title"],
            Vixly.Shared.Localization.VixlyText.Format(text, "ui.maintenance.page_body", "time", window.EndsUtc.UtcDateTime.ToString("g", System.Globalization.CultureInfo.CurrentCulture)),
            window.Message(language));
    }
}
