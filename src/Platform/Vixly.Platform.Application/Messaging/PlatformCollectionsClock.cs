using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Platform.Domain.Collections;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformCollectionsClock.cs
//
// PURPOSE:
// Runs collections for every tenant that needs it (D-C18).
//
// RESPONSIBILITIES:
// - Walk the tenants to check in batches of 50
// - Check each tenant in its own commit; a refusal or lost race is undone and left for the next run
//
// IMPORTANT NOTES:
// - Runs in the subscription clock host until the Platform Worker opens (ADR-0007)
// - Safe to run on two hosts: row versions and unique keys decide
// =========================================================
public sealed class PlatformCollectionsClock
{
    private const int MaxBatches = 40;

    private readonly PlatformCollectionsAdministration _collections;

    public PlatformCollectionsClock(PlatformCollectionsAdministration collections)
    {
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        int handled = 0;
        Guid? after = null;

        for (int batch = 0; batch < MaxBatches; batch++)
        {
            IReadOnlyList<Guid> tenants = await _collections.Store.ListTenantsToCheckAsync(_collections.UtcNow, after, PlatformCollectionsLimits.BatchSize, cancellationToken);

            foreach (Guid tenantId in tenants)
            {
                try
                {
                    handled += await _collections.CheckTenantAsync(null, tenantId, cancellationToken) ? 1 : 0;
                }
                catch (DomainException)
                {
                    _collections.Discard();
                }
            }

            if (tenants.Count < PlatformCollectionsLimits.BatchSize)
            {
                break;
            }

            after = tenants[^1];
        }

        return handled;
    }
}
