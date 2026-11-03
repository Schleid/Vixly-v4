namespace Vixly.Platform.Domain.Maintenance;

// =========================================================
// FILE: PlatformMaintenanceSites.cs
//
// PURPOSE:
// The sites a maintenance window closes (owner, Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Name each site Vixly IT can close
//
// IMPORTANT NOTES:
// - Flags: a window may close several
// - Control is never closed, so IT can always end a window
// - Wire codes: public, companies, customers, staff
// =========================================================
[Flags]
public enum PlatformMaintenanceSites
{
    None = 0,
    PublicSite = 1,
    CompanyWebsites = 2,
    CustomerPortals = 4,
    StaffWorkspaces = 8,
    All = PublicSite | CompanyWebsites | CustomerPortals | StaffWorkspaces
}
