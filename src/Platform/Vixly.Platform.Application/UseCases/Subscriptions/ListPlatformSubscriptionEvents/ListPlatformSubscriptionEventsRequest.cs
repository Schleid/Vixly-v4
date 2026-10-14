namespace Vixly.Platform.Application.UseCases.Subscriptions.ListPlatformSubscriptionEvents;

// =========================================================
// FILE: ListPlatformSubscriptionEventsRequest.cs
//
// PURPOSE:
// Pages a tenant's subscription history, newest first.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformSubscriptionEventsRequest
{
    public Guid PlatformTenantId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
