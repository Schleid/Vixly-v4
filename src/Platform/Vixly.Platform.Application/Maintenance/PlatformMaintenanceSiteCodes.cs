using Vixly.Platform.Domain.Maintenance;

namespace Vixly.Platform.Application.Maintenance;

// =========================================================
// FILE: PlatformMaintenanceSiteCodes.cs
//
// PURPOSE:
// The wire codes of the sites a maintenance window closes.
//
// RESPONSIBILITIES:
// - Read and write a site code
//
// IMPORTANT NOTES:
// - public, companies, customers, staff; one site per code
// =========================================================
public static class PlatformMaintenanceSiteCodes
{
    public const string PublicSite = "public";

    public const string CompanyWebsites = "companies";

    public const string CustomerPortals = "customers";

    public const string StaffWorkspaces = "staff";

    public static IReadOnlyList<(string Code, PlatformMaintenanceSites Site)> All { get; } = new[]
    {
        (PublicSite, PlatformMaintenanceSites.PublicSite),
        (CompanyWebsites, PlatformMaintenanceSites.CompanyWebsites),
        (CustomerPortals, PlatformMaintenanceSites.CustomerPortals),
        (StaffWorkspaces, PlatformMaintenanceSites.StaffWorkspaces)
    };

    public static PlatformMaintenanceSites? Parse(string? code)
    {
        foreach ((string known, PlatformMaintenanceSites site) in All)
        {
            if (string.Equals(known, code?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return site;
            }
        }

        return null;
    }

    public static IReadOnlyList<string> Of(PlatformMaintenanceSites sites)
    {
        return All.Where(item => (sites & item.Site) != PlatformMaintenanceSites.None).Select(item => item.Code).ToArray();
    }
}
