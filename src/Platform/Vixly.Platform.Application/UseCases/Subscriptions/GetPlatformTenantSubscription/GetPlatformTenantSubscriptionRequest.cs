namespace Vixly.Platform.Application.UseCases.Subscriptions.GetPlatformTenantSubscription;

// =========================================================
// FILE: GetPlatformTenantSubscriptionRequest.cs
//
// PURPOSE:
// Reads a tenant's current subscription for the tenant page.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformTenantSubscriptionRequest
{
    public Guid PlatformTenantId { get; init; }

    public string? Language { get; init; }
}
