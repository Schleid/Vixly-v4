using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: IPlatformAuditQueries.cs
//
// PURPOSE:
// Read-only projections for the audit log screens.
//
// RESPONSIBILITIES:
// - Page through audit entries, newest first, with filters
// - Load one entry with its before, after and arguments
//
// IMPORTANT NOTES:
// - No tracking; audit rows are never changed (D-2)
// =========================================================
public interface IPlatformAuditQueries
{
    Task<PagedResult<PlatformAuditListItem>> ListAsync(
        PlatformAuditFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken);

    Task<PlatformAuditEntryDetail?> GetAsync(Guid auditEntryId, CancellationToken cancellationToken);
}
