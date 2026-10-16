using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformTermsAccepted.cs
//
// PURPOSE:
// Event for Operations: a tenant accepted a Terms version (D-Lg11).
//
// RESPONSIBILITIES:
// - Carry the tenant, document, version and source
//
// IMPORTANT NOTES:
// - Version 1
// =========================================================
public sealed record PlatformTermsAccepted(Guid PlatformTenantId, Guid DocumentId, string Version, PlatformTermsAcceptanceSource Source, DateTimeOffset OccurredUtc);
