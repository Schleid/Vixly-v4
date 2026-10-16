using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Licensing;

// =========================================================
// FILE: PlatformPortalAccess.cs
//
// PURPOSE:
// What Edge and Operations learn about a portal slug (D-L10).
//
// RESPONSIBILITIES:
// - Carry the tenant, its status and the subscription access level
//
// IMPORTANT NOTES:
// - Unknown, never-active and closed tenants give the same answer: Found is false
// =========================================================
public sealed record PlatformPortalAccess(
    bool Found,
    Guid? PlatformTenantId,
    PlatformTenantStatus? Status,
    PlatformSubscriptionAccess Access,
    string? CanonicalSlug = null,
    string? DefaultLanguage = null,
    Vixly.Platform.Domain.Legal.PlatformLegalStatus Legal = Vixly.Platform.Domain.Legal.PlatformLegalStatus.NotRequired);
