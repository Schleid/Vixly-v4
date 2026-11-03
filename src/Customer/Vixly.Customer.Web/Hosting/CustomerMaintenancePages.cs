using Microsoft.Extensions.Localization;
using Vixly.Customer.Resources;
using Vixly.Shared.Hosting.Maintenance;

namespace Vixly.Customer.Web.Hosting;

// =========================================================
// FILE: CustomerMaintenancePages.cs
//
// PURPOSE:
// Closes the customer portals during a maintenance window (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Say which requests the window closes
// - Give the maintenance page its words
//
// IMPORTANT NOTES:
// - The words never say "Vixly" (ADR-0012 W-1)
// =========================================================
public static class CustomerMaintenancePages
{
    public const string Site = "customers";

    public static string? SiteOf(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        return VixlyMaintenance.IsAlwaysOpen(http.Request.Path) ? null : Site;
    }

    public static VixlyMaintenancePageText Text(HttpContext http, VixlyMaintenanceWindow window)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(window);

        IStringLocalizer<CustomerText> text = http.RequestServices.GetRequiredService<IStringLocalizer<CustomerText>>();
        string language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        return new VixlyMaintenancePageText(language, text["ui.maintenance.page_title"],
            Vixly.Shared.Localization.VixlyText.Format(text, "ui.maintenance.page_body", "time", window.EndsUtc.UtcDateTime.ToString("g", System.Globalization.CultureInfo.CurrentCulture)),
            window.Message(language));
    }
}
