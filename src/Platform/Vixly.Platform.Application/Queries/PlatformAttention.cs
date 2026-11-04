namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformAttention.cs
//
// PURPOSE:
// What waits for Control staff, counted beside the menu (owner, 2026-11-04).
//
// RESPONSIBILITIES:
// - Name the counted pages: approvals, Zelle reports, company changes
//
// IMPORTANT NOTES:
// - One count per menu entry
// =========================================================
public enum PlatformAttention
{
    Approvals = 0,
    Zelle = 1,
    TenantChanges = 2
}
