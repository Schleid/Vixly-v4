namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformTenantCompanyChangeMessage.cs
//
// PURPOSE:
// Outbox payload for a company change request that was submitted or decided (B12).
//
// RESPONSIBILITIES:
// - Name the request; the handlers read the rest
//
// IMPORTANT NOTES:
// - Internal message; never published to other planes
// =========================================================
public sealed record PlatformTenantCompanyChangeMessage(Guid RequestId, Guid PlatformTenantId, DateTimeOffset OccurredUtc);
