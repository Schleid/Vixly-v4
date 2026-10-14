namespace Vixly.Platform.Application.UseCases.Subscriptions.ResumePlatformSubscription;

// =========================================================
// FILE: ResumePlatformSubscriptionRequest.cs
//
// PURPOSE:
// Withdraws a requested cancellation before the period ends.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ResumePlatformSubscriptionRequest
{
    public Guid PlatformTenantId { get; init; }
}
