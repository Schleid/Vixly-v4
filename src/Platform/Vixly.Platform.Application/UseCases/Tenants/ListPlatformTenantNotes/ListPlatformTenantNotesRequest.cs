namespace Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantNotes;

// =========================================================
// FILE: ListPlatformTenantNotesRequest.cs
//
// PURPOSE:
// Input for ListPlatformTenantNotes.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Page and PageSize are clamped by PagedRequest
// =========================================================
public sealed class ListPlatformTenantNotesRequest
{
    public Guid PlatformTenantId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
