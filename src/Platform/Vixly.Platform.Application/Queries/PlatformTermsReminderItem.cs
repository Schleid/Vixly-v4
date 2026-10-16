using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTermsReminderItem.cs
//
// PURPOSE:
// One Terms reminder sent to a tenant.
//
// RESPONSIBILITIES:
// - Carry the step, version, language, when and who
//
// IMPORTANT NOTES:
// - Read model
// =========================================================
public sealed record PlatformTermsReminderItem(PlatformTermsReminderStep Step, string Version, string Language, DateTimeOffset SentUtc, string? SentByName, string? Reason);
