namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformRegistrationAccountSetupReady.cs
//
// PURPOSE:
// Outbox payload: a registration moved to "pending account setup" (Operations Slice 1, section 8).
//
// RESPONSIBILITIES:
// - Carry the tenant and registration ids
//
// IMPORTANT NOTES:
// - Ids only: the feed is not split by tenant, so it never carries names or emails
// - Written when the registration is paid, or at once for a trial or a plan with nothing due
// =========================================================
public sealed record PlatformRegistrationAccountSetupReady(Guid PlatformTenantId, Guid RegistrationId, DateTimeOffset OccurredUtc);
