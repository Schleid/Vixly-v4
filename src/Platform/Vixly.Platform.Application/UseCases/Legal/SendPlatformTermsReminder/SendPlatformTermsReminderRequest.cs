namespace Vixly.Platform.Application.UseCases.Legal.SendPlatformTermsReminder;

// =========================================================
// FILE: SendPlatformTermsReminderRequest.cs
//
// PURPOSE:
// Sends a Terms reminder to a tenant's owner now, with a reason (D-Lg7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SendPlatformTermsReminderRequest
{
    public Guid PlatformTenantId { get; init; }

    public string? Reason { get; init; }
}
