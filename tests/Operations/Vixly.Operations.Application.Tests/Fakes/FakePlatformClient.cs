using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakePlatformClient.cs
//
// PURPOSE:
// Stands in for Platform in use case tests.
//
// RESPONSIBILITIES:
// - Answer portal lookups and serve event pages
//
// IMPORTANT NOTES:
// - Down makes every call throw as if Platform were unreachable
// =========================================================
internal sealed class FakePlatformClient : IOperationsPlatformClient
{
    public Dictionary<string, OperationsPortalAnswer> Portals { get; } = new(StringComparer.Ordinal);

    public Queue<OperationsPlatformEventPage> Pages { get; } = new();

    public List<long> ReadFrom { get; } = new();

    public bool Down { get; set; }

    public int PortalCalls { get; private set; }

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
        ReadFrom.Add(after);

        if (Down)
        {
            throw new OperationsPlatformUnavailableException("down");
        }

        return Task.FromResult(Pages.Count > 0 ? Pages.Dequeue() : new OperationsPlatformEventPage(Array.Empty<OperationsPlatformEvent>(), after, false));
    }
}
