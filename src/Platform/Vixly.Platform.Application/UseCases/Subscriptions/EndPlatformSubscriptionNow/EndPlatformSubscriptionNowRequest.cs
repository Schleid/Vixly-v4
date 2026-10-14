namespace Vixly.Platform.Application.UseCases.Subscriptions.EndPlatformSubscriptionNow;

// =========================================================
// FILE: EndPlatformSubscriptionNowRequest.cs
//
// PURPOSE:
// Ends a subscription at once.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class EndPlatformSubscriptionNowRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
