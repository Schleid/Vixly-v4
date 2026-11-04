namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantChangeStatus.cs
//
// PURPOSE:
// Where a tenant’s company change request stands (B12).
//
// RESPONSIBILITIES:
// - Name each state of a change request
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// =========================================================
public enum PlatformTenantChangeStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Withdrawn = 4
}
