using Microsoft.Extensions.Localization;
using Vixly.Operations.Resources;
using Vixly.Shared.Hosting.Maintenance;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsMaintenancePages.cs
//
// PURPOSE:
// Closes the company websites and staff workspaces during a maintenance window (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Say which site a request belongs to
// - Give the maintenance page its words
//
// IMPORTANT NOTES:
// - /staff, /agent and /maps are the staff workspace; the rest is the company website
// - The words never say "Vixly" (ADR-0012 W-1)
// =========================================================
public static class OperationsMaintenancePages
{
    public const string Staff = "staff";

    public const string Companies = "companies";

    public static string? Site(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        PathString path = http.Request.Path;

        if (VixlyMaintenance.IsAlwaysOpen(path))
        {
            return null;
        }

        return path.StartsWithSegments("/staff", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/agent", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/maps", StringComparison.OrdinalIgnoreCase)
            ? Staff
            : Companies;
    }

    public static VixlyMaintenancePageText Text(HttpContext http, VixlyMaintenanceWindow window)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(window);

        IStringLocalizer<OperationsText> text = http.RequestServices.GetRequiredService<IStringLocalizer<OperationsText>>();
        string language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        return new VixlyMaintenancePageText(language, text["ui.maintenance.page_title"],
            Vixly.Shared.Localization.VixlyText.Format(text, "ui.maintenance.page_body", "time", window.EndsUtc.UtcDateTime.ToString("g", System.Globalization.CultureInfo.CurrentCulture)),
            window.Message(language));
    }
}
