namespace Vixly.Platform.Web.Features.Maintenance;

// =========================================================
// FILE: MaintenanceFormModel.cs
//
// PURPOSE:
// The fields of a new maintenance window.
//
// RESPONSIBILITIES:
// - Hold what the person types until it is sent
//
// IMPORTANT NOTES:
// - Times are typed in UTC
// - Starts tomorrow at 02:00 UTC for two hours by default
// =========================================================
public sealed class MaintenanceFormModel
{
    public DateTime StartsUtc { get; set; } = DateTime.UtcNow.Date.AddDays(1).AddHours(2);

    public DateTime EndsUtc { get; set; } = DateTime.UtcNow.Date.AddDays(1).AddHours(4);

    public bool PublicSite { get; set; } = true;

    public bool CompanyWebsites { get; set; } = true;

    public bool CustomerPortals { get; set; } = true;

    public bool StaffWorkspaces { get; set; } = true;

    public int NoticeHours { get; set; } = 48;

    public string? MessageEn { get; set; }

    public string? MessageFr { get; set; }

    public string? MessageEs { get; set; }

    public IReadOnlyList<string> Sites()
    {
        List<string> sites = new();

        if (PublicSite)
        {
            sites.Add(Vixly.Platform.Application.Maintenance.PlatformMaintenanceSiteCodes.PublicSite);
        }

        if (CompanyWebsites)
        {
            sites.Add(Vixly.Platform.Application.Maintenance.PlatformMaintenanceSiteCodes.CompanyWebsites);
        }

        if (CustomerPortals)
        {
            sites.Add(Vixly.Platform.Application.Maintenance.PlatformMaintenanceSiteCodes.CustomerPortals);
        }

        if (StaffWorkspaces)
        {
            sites.Add(Vixly.Platform.Application.Maintenance.PlatformMaintenanceSiteCodes.StaffWorkspaces);
        }

        return sites;
    }
}
