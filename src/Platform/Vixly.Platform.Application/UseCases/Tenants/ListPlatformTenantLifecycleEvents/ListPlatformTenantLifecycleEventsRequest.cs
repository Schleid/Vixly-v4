namespace Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantLifecycleEvents;

// =========================================================
// FILE: ListPlatformTenantLifecycleEventsRequest.cs
//
// PURPOSE:
// Input for ListPlatformTenantLifecycleEvents.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Page and PageSize are clamped by PagedRequest
// =========================================================
public sealed class ListPlatformTenantLifecycleEventsRequest
{
    public Guid PlatformTenantId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
