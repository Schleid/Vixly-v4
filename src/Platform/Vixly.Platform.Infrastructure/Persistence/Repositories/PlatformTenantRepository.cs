using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformTenantRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformTenantRepository.
//
// RESPONSIBILITIES:
// - Load tenants with their owned contacts, tags and suspensions
// - Add tenants, history events and notes
//
// IMPORTANT NOTES:
// - History and notes are never loaded here
// =========================================================
internal sealed class PlatformTenantRepository : IPlatformTenantRepository
{
    private readonly PlatformDbContext _context;

    public PlatformTenantRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformTenant?> GetAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return _context.Tenants.FirstOrDefaultAsync(tenant => tenant.Id == platformTenantId, cancellationToken);
    }

    public Task<bool> CodeExistsAsync(string tenantCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenantCode);

        return _context.Tenants.AnyAsync(tenant => tenant.TenantCode == tenantCode, cancellationToken);
    }

    public void Add(PlatformTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        _context.Tenants.Add(tenant);
    }

    public void AddEvents(IEnumerable<PlatformTenantLifecycleEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        _context.TenantLifecycleEvents.AddRange(events);
    }

    public void AddNote(PlatformTenantNote note)
    {
        ArgumentNullException.ThrowIfNull(note);

        _context.TenantNotes.Add(note);
    }
}
