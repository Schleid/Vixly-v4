using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.UseCases.Legal;

// =========================================================
// FILE: PlatformLegalMail.cs
//
// PURPOSE:
// What a Terms email says.
//
// RESPONSIBILITIES:
// - Carry the step, version, deadline, summary and the portal link
//
// IMPORTANT NOTES:
// - Deadline is null for notice-only versions
// =========================================================
public sealed record PlatformLegalMail(PlatformTermsReminderStep Step, string Version, bool AcceptanceRequired, DateTimeOffset? DeadlineUtc, string? Summary, string Link, bool Overdue = false);
