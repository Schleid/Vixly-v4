using Vixly.Platform.Application.Collections;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformCollectionsNotice.cs
//
// PURPOSE:
// Outbox message: a collections email is due for a tenant (D-C11).
//
// RESPONSIBILITIES:
// - Name the tenant, the step and the dates; the handler reads the invoices when it sends
//
// IMPORTANT NOTES:
// - Written in the same commit as the step; sent once by the outbox
// - CaseId is empty for a Pending payment block without a case (D-C5)
// =========================================================
public sealed record PlatformCollectionsNotice(
    Guid PlatformTenantId,
    Guid? CaseId,
    PlatformCollectionsNoticeKind Kind,
    DateTimeOffset? BlockDateUtc,
    DateTime? ExtendedThrough,
    DateTimeOffset OccurredUtc);
