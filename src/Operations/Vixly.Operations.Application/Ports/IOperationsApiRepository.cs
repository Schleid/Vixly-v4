using Vixly.Operations.Application.Api;
using Vixly.Operations.Domain.Api;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsApiRepository.cs
//
// PURPOSE:
// The company’s API keys, webhook addresses, events and deliveries (Slice 10).
//
// RESPONSIBILITIES:
// - Find, add and list keys and addresses
// - Read events after a place; queue deliveries and move the place in one transaction
// - Claim a delivery, record its result under the claim; note an address’s success or failure; prune
//
// IMPORTANT NOTES:
// - Tenant-filtered; the worker lists companies and prunes events under the system scope
// - Queueing, claiming and recording are each one atomic step: several hosts may run the worker
// =========================================================
public interface IOperationsApiRepository
{
    Task<IReadOnlyList<OperationsApiKey>> ListKeysAsync(CancellationToken cancellationToken);

    Task<OperationsApiKey?> FindKeyAsync(string keyId, CancellationToken cancellationToken);

    void Add(OperationsApiKey key);

    Task<IReadOnlyList<OperationsWebhookEndpoint>> ListEndpointsAsync(CancellationToken cancellationToken);

    Task<OperationsWebhookEndpoint?> GetEndpointAsync(Guid endpointId, CancellationToken cancellationToken);

    void Add(OperationsWebhookEndpoint endpoint);

    void Remove(OperationsWebhookEndpoint endpoint);

    // The newest event version every save before it has finished with (a new address starts here).
    Task<long> CurrentVersionAsync(CancellationToken cancellationToken);

    // Events of a kind after a version, only up to the oldest save still in progress, oldest first.
    Task<IReadOnlyList<OperationsApiEventRow>> EventsAfterAsync(OperationsWebhookEvents kind, long afterVersion, int take, CancellationToken cancellationToken);

    // Adds the deliveries and moves the address from one version to the next, together; false when another round moved it first.
    Task<bool> QueueAsync(Guid endpointId, OperationsWebhookEvents kind, long fromVersion, long toVersion, IReadOnlyList<OperationsWebhookDelivery> deliveries,
        CancellationToken cancellationToken);

    void Add(OperationsWebhookDelivery delivery);

    Task<OperationsWebhookDelivery?> GetDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsWebhookDelivery>> ListDeliveriesAsync(int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> DueDeliveryIdsAsync(DateTimeOffset utcNow, int take, CancellationToken cancellationToken);

    // The delivery when this sender took it (due and free), else none.
    Task<OperationsWebhookDelivery?> ClaimAsync(Guid deliveryId, Guid token, DateTimeOffset until, DateTimeOffset utcNow, CancellationToken cancellationToken);

    // Writes the delivery's result, only while this sender still holds it.
    Task RecordAsync(OperationsWebhookDelivery delivery, Guid token, CancellationToken cancellationToken);

    Task EndpointDeliveredAsync(Guid endpointId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    // True when this failure turns the address off (failing for three days).
    Task<bool> EndpointFailedAsync(Guid endpointId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<int> PruneDeliveriesAsync(DateTimeOffset before, CancellationToken cancellationToken);

    // System scope: events older than a week, for every company.
    Task<int> PruneEventsAsync(DateTimeOffset before, CancellationToken cancellationToken);

    // System scope: the companies with at least one active address.
    Task<IReadOnlyList<Guid>> TenantsWithActiveEndpointsAsync(CancellationToken cancellationToken);
}
