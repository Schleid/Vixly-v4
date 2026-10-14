using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformTenantRepository.cs
//
// PURPOSE:
// Loads and stores tenants and their history events.
//
// RESPONSIBILITIES:
// - Load a tenant with its contacts, tags and suspensions
// - Add new tenants and history events
// - Check whether a tenant code is used
//
// IMPORTANT NOTES:
// - History events are only added, never loaded here (D-2)
// - Changes are saved by IPlatformUnitOfWork
// =========================================================
public interface IPlatformTenantRepository
{
    Task<PlatformTenant?> GetAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string tenantCode, CancellationToken cancellationToken);

    void Add(PlatformTenant tenant);

    void AddEvents(IEnumerable<PlatformTenantLifecycleEvent> events);

    void AddNote(PlatformTenantNote note);
}
