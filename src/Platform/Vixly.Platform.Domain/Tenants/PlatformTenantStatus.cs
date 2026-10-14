namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantStatus.cs
//
// PURPOSE:
// The lifecycle state of a tenant (Slice 2, D-T1).
//
// RESPONSIBILITIES:
// - Name the values the model accepts
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Banned is terminal
// - Pending activation is entered by Slice 5 (payment done)
// =========================================================
public enum PlatformTenantStatus
{
    Draft = 0,
    PendingActivation = 1,
    Active = 2,
    Suspended = 3,
    Deactivated = 4,
    Banned = 5
}
