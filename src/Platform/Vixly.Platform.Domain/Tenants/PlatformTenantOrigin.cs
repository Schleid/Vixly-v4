namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantOrigin.cs
//
// PURPOSE:
// How a tenant came to exist.
//
// RESPONSIBILITIES:
// - Name the values the model accepts
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Staff: created from the console for sales-led onboarding (D-T8)
// =========================================================
public enum PlatformTenantOrigin
{
    SelfRegistration = 0,
    Staff = 1
}
