using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformLegalNotice.cs
//
// PURPOSE:
// Outbox message: email a Terms notice or reminder to a tenant's owner.
//
// RESPONSIBILITIES:
// - Carry the tenant, document and step
//
// IMPORTANT NOTES:
// - Version 1
// =========================================================
public sealed record PlatformLegalNotice(Guid PlatformTenantId, Guid DocumentId, PlatformTermsReminderStep Step, DateTimeOffset OccurredUtc);
