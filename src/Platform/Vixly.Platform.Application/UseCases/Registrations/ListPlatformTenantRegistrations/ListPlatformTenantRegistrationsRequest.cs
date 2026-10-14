using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.Registrations.ListPlatformTenantRegistrations;

// =========================================================
// FILE: ListPlatformTenantRegistrationsRequest.cs
//
// PURPOSE:
// Input for ListPlatformTenantRegistrations.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Page and PageSize are clamped by PagedRequest
// =========================================================
public sealed class ListPlatformTenantRegistrationsRequest
{
    public PlatformTenantRegistrationStatus? Status { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
