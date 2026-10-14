namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantCreation.cs
//
// PURPOSE:
// A new tenant together with the history events of its creation.
//
// RESPONSIBILITIES:
// - Hand both to the use case, which adds them in one commit
//
// IMPORTANT NOTES:
// - Returned by the PlatformTenant factories only
// =========================================================
public sealed record PlatformTenantCreation(
    PlatformTenant Tenant,
    IReadOnlyList<PlatformTenantLifecycleEvent> Events);
