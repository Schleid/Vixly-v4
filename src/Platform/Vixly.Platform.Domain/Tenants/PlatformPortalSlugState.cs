namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformPortalSlugState.cs
//
// PURPOSE:
// Whether a portal slug is only claimed or belongs to a tenant.
//
// RESPONSIBILITIES:
// - Name the values the model accepts
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Pending: held by an unverified registration until its hold lapses
// - Permanent: the address of a tenant, never released (D-T11)
// =========================================================
public enum PlatformPortalSlugState
{
    Pending = 0,
    Permanent = 1
}
