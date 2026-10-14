namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformTenantSlugCorrected.cs
//
// PURPOSE:
// The payload of platform.tenant.slug_corrected.v1.
//
// RESPONSIBILITIES:
// - Say which tenant's address was corrected before it went live
//
// IMPORTANT NOTES:
// - Edge drops any cached resolution of the old slug (Stage 5)
// =========================================================
public sealed record PlatformTenantSlugCorrected(
    Guid PlatformTenantId,
    string FromSlug,
    string ToSlug,
    DateTimeOffset OccurredUtc);
