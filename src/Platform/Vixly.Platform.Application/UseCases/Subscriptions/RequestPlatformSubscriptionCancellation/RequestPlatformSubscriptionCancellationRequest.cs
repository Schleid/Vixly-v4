namespace Vixly.Platform.Application.UseCases.Subscriptions.RequestPlatformSubscriptionCancellation;

// =========================================================
// FILE: RequestPlatformSubscriptionCancellationRequest.cs
//
// PURPOSE:
// Cancels at the period end, or at once for a trial or an unpaid subscription.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RequestPlatformSubscriptionCancellationRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
