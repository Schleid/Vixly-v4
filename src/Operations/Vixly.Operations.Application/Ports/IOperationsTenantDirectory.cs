using Vixly.Operations.Application.Platform;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsTenantDirectory.cs
//
// PURPOSE:
// Platform's answer for a host name, cached (Stage 3, Slice 0, D-F2).
//
// RESPONSIBILITIES:
// - Answer from the cache for 60 seconds, then ask Platform again
// - Forget a tenant at once when its status or address changes
//
// IMPORTANT NOTES:
// - Keyed by slug; also indexed by tenant id so the feed can forget it
// =========================================================
public interface IOperationsTenantDirectory
{
    Task<OperationsPortalAnswer> FindAsync(string slug, CancellationToken cancellationToken);

    void Forget(Guid tenantId);
}
