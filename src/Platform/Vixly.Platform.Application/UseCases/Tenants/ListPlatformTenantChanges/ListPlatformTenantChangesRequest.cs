using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantChanges;

// =========================================================
// FILE: ListPlatformTenantChangesRequest.cs
//
// PURPOSE:
// Filter and page of the company change queue (B12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformTenantChangesRequest
{
    public PlatformTenantChangeStatus? Status { get; init; } = PlatformTenantChangeStatus.Pending;

    public Guid? PlatformTenantId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
