using Vixly.Platform.Application.Queries;

namespace Vixly.Platform.Application.UseCases.Legal;

// =========================================================
// FILE: PlatformTenantLegalDetail.cs
//
// PURPOSE:
// The Legal section of a tenant page.
//
// RESPONSIBILITIES:
// - Carry the status, acceptances, reminders and abilities
//
// IMPORTANT NOTES:
// - Evidence files are not carried
// =========================================================
public sealed record PlatformTenantLegalDetail(
    PlatformTenantLegalState State,
    IReadOnlyList<PlatformTermsAcceptanceItem> Acceptances,
    IReadOnlyList<PlatformTermsReminderItem> Reminders,
    PlatformLegalAbilities Abilities);
