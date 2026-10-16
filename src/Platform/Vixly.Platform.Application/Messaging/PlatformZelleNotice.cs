using Vixly.Platform.Application.UseCases.Payments;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformZelleNotice.cs
//
// PURPOSE:
// Outbox message: send a Zelle email about one report.
//
// RESPONSIBILITIES:
// - Carry the tenant, the report and which email
//
// IMPORTANT NOTES:
// - Version 1
// =========================================================
public sealed record PlatformZelleNotice(Guid PlatformTenantId, Guid ClaimId, PlatformZelleNoticeKind Kind, DateTimeOffset OccurredUtc);
