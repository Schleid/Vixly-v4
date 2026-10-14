using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Audit;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformAuditRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformAuditRepository.
//
// RESPONSIBILITIES:
// - Add audit entries to the current unit of work
//
// IMPORTANT NOTES:
// - Add only, by design
// =========================================================
internal sealed class PlatformAuditRepository : IPlatformAuditRepository
{
    private readonly PlatformDbContext _context;

    public PlatformAuditRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(PlatformAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _context.AuditEntries.Add(entry);
    }
}
