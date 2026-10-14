using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: IPlatformTenantQueries.cs
//
// PURPOSE:
// Read-only views of tenants for the console.
//
// RESPONSIBILITIES:
// - List tenants, paged and filtered (P-1)
// - Read one tenant, its history, its notes
// - List staff who may manage accounts
// - List and read registrations
//
// IMPORTANT NOTES:
// - Projections only: no tracked entities
// - Every list is paged
// =========================================================
public interface IPlatformTenantQueries
{
    Task<PagedResult<PlatformTenantListItem>> ListAsync(
        PlatformTenantListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken);

    Task<PlatformTenantDetail?> GetAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<PagedResult<PlatformTenantEventItem>> ListEventsAsync(
        Guid platformTenantId,
        PagedRequest page,
        CancellationToken cancellationToken);

    Task<PagedResult<PlatformTenantNoteItem>> ListNotesAsync(
        Guid platformTenantId,
        PagedRequest page,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformStaffOptionItem>> ListActiveStaffAsync(CancellationToken cancellationToken);

    Task<PagedResult<PlatformTenantRegistrationListItem>> ListRegistrationsAsync(
        PlatformTenantRegistrationStatus? status,
        string? search,
        PagedRequest page,
        CancellationToken cancellationToken);

    Task<PlatformTenantRegistrationDetail?> GetRegistrationAsync(Guid registrationId, CancellationToken cancellationToken);
}
