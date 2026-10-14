using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenants;

// =========================================================
// FILE: ListPlatformTenantsRequest.cs
//
// PURPOSE:
// Input for ListPlatformTenants.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Page and PageSize are clamped by PagedRequest
// =========================================================
public sealed class ListPlatformTenantsRequest
{
    public string? Search { get; init; }

    public PlatformTenantStatus? Status { get; init; }

    public string? Tag { get; init; }

    public Guid? AccountManagerUserId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
