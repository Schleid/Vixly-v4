using Vixly.Operations.Application.Platform;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsPlatformClient.cs
//
// PURPOSE:
// Calls Platform.Api for Operations (Stage 3, Slice 0, D-F3).
//
// RESPONSIBILITIES:
// - Look up a host name
// - Read the event feed
//
// IMPORTANT NOTES:
// - Every call carries a signed service token (ADR-0014)
// - Throws OperationsPlatformUnavailableException when Platform does not answer
// =========================================================
public interface IOperationsPlatformClient
{
    Task<OperationsPortalAnswer> GetPortalAsync(string slug, CancellationToken cancellationToken);

    Task<OperationsPlatformEventPage> ReadEventsAsync(long after, int limit, CancellationToken cancellationToken);

    // GET /internal/v1/tenant as the tenant (Slice 1): company names, default language and the owner contact.
    Task<OperationsTenantAccount?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}
