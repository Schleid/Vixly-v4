using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Api.Tests;

// =========================================================
// FILE: FakePlatformClient.cs
//
// PURPOSE:
// Stands in for Platform behind the real host.
//
// RESPONSIBILITIES:
// - Answer portal lookups
// - Act as unreachable when Down
//
// IMPORTANT NOTES:
// - Replaces the HTTP client in the test host
// =========================================================
internal sealed class FakePlatformClient : IOperationsPlatformClient
{
    public Dictionary<string, OperationsPortalAnswer> Portals { get; } = new(StringComparer.Ordinal);

    public bool Down { get; set; }

    public int PortalCalls { get; private set; }

    public void Tenant(string slug, Guid tenantId, string status, string access = "full", string language = "en")
    {
        Portals[slug] = new OperationsPortalAnswer(true, tenantId, status, access, slug, language, "accepted");
    }

    public Task<OperationsPortalAnswer> GetPortalAsync(string slug, CancellationToken cancellationToken)
    {
        PortalCalls++;

        if (Down)
        {
            throw new OperationsPlatformUnavailableException("down");
        }

        return Task.FromResult(Portals.TryGetValue(slug, out OperationsPortalAnswer? answer)
            ? answer
            : new OperationsPortalAnswer(false, null, null, "none", null, null, "none"));
    }

    public Dictionary<Guid, OperationsTenantAccount> Accounts { get; } = new();

    public Task<OperationsTenantAccount?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (Down)
        {
            throw new OperationsPlatformUnavailableException("down");
        }

        return Task.FromResult(Accounts.TryGetValue(tenantId, out OperationsTenantAccount? account) ? account : null);
    }

    public Task<OperationsPlatformEventPage> ReadEventsAsync(long after, int limit, CancellationToken cancellationToken)
    {
        return Task.FromResult(new OperationsPlatformEventPage(Array.Empty<OperationsPlatformEvent>(), after, false));
    }
}
