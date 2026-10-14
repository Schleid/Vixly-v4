namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantContactRole.cs
//
// PURPOSE:
// The role of a named tenant contact (D-T15).
//
// RESPONSIBILITIES:
// - Name the values the model accepts
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Owner is required; Billing and Technical fall back to Owner
// =========================================================
public enum PlatformTenantContactRole
{
    Owner = 0,
    Billing = 1,
    Technical = 2
}
