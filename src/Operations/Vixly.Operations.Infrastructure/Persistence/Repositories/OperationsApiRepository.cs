using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Api;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsApiRepository.cs
//
// PURPOSE:
// Stores the company’s API keys, webhook addresses, events and deliveries (Slice 10).
//
// RESPONSIBILITIES:
// - Find, add and list keys and addresses; read events after a version
// - Queue deliveries with the place in one transaction; claim and record deliveries; note an address’s success or failure; prune
//
// IMPORTANT NOTES:
// - Tenant-filtered by the model (D-F1); raw SQL names the tenant itself
// - Events are read only below MIN_ACTIVE_ROWVERSION(): a save still running can never be passed over
// - Claims and results are single conditional updates: safe with several hosts
// =========================================================
internal sealed class OperationsApiRepository : IOperationsApiRepository
{
    private readonly OperationsDbContext _context;

    private readonly IOperationsTenantScope _scope;

    public OperationsApiRepository(OperationsDbContext context, IOperationsTenantScope scope)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public async Task<IReadOnlyList<OperationsApiKey>> ListKeysAsync(CancellationToken cancellationToken) =>
        await _context.ApiKeys.OrderByDescending(key => key.CreatedUtc).Take(200).ToListAsync(cancellationToken);

    public Task<OperationsApiKey?> FindKeyAsync(string keyId, CancellationToken cancellationToken) =>
        _context.ApiKeys.FirstOrDefaultAsync(key => key.KeyId == keyId, cancellationToken);

    public void Add(OperationsApiKey key) => _context.ApiKeys.Add(key ?? throw new ArgumentNullException(nameof(key)));

    public async Task<IReadOnlyList<OperationsWebhookEndpoint>> ListEndpointsAsync(CancellationToken cancellationToken) =>
        await _context.WebhookEndpoints.OrderBy(endpoint => endpoint.CreatedUtc).ToListAsync(cancellationToken);

    public Task<OperationsWebhookEndpoint?> GetEndpointAsync(Guid endpointId, CancellationToken cancellationToken) =>
        _context.WebhookEndpoints.FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);

    public void Add(OperationsWebhookEndpoint endpoint) => _context.WebhookEndpoints.Add(endpoint ?? throw new ArgumentNullException(nameof(endpoint)));

    // Its deliveries go with it (cascade in the database).
    public void Remove(OperationsWebhookEndpoint endpoint) => _context.WebhookEndpoints.Remove(endpoint ?? throw new ArgumentNullException(nameof(endpoint)));

    public async Task<long> CurrentVersionAsync(CancellationToken cancellationToken) =>
        (await _context.Database.SqlQueryRaw<long>("SELECT CAST(MIN_ACTIVE_ROWVERSION() AS bigint) - 1 AS [Value]").ToListAsync(cancellationToken)).Single();

    public async Task<IReadOnlyList<OperationsApiEventRow>> EventsAfterAsync(OperationsWebhookEvents kind, long afterVersion, int take, CancellationToken cancellationToken)
    {
        Guid tenantId = Tenant();
        int count = Math.Clamp(take, 1, 500);
        int value = (int)kind;
        return await _context.Database.SqlQuery<OperationsApiEventRow>($"""
            SELECT TOP ({count}) [Id], [SourceId], CAST([Version] AS bigint) AS [Version], [CreatedUtc]
            FROM [ops].[ApiEvents]
            WHERE [TenantId] = {tenantId} AND [Kind] = {value} AND [Version] > CAST({afterVersion} AS binary(8)) AND [Version] < MIN_ACTIVE_ROWVERSION()
            ORDER BY [Version]
            """).ToListAsync(cancellationToken);
    }

    public async Task<bool> QueueAsync(Guid endpointId, OperationsWebhookEvents kind, long fromVersion, long toVersion, IReadOnlyList<OperationsWebhookDelivery> deliveries,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deliveries);

        // The retrying execution strategy needs the whole transaction as one unit it can repeat.
        return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            IQueryable<OperationsWebhookEndpoint> endpoint = _context.WebhookEndpoints.Where(item => item.Id == endpointId);
            int moved = kind switch
            {
                OperationsWebhookEvents.Tracking => await endpoint.Where(item => item.TrackingAfter == fromVersion)
                    .ExecuteUpdateAsync(set => set.SetProperty(item => item.TrackingAfter, toVersion), cancellationToken),
                OperationsWebhookEvents.CustomerCreated => await endpoint.Where(item => item.CustomersAfter == fromVersion)
                    .ExecuteUpdateAsync(set => set.SetProperty(item => item.CustomersAfter, toVersion), cancellationToken),
                OperationsWebhookEvents.InvoiceIssued => await endpoint.Where(item => item.InvoicesAfter == fromVersion)
                    .ExecuteUpdateAsync(set => set.SetProperty(item => item.InvoicesAfter, toVersion), cancellationToken),
                OperationsWebhookEvents.PaymentRecorded => await endpoint.Where(item => item.PaymentsAfter == fromVersion)
                    .ExecuteUpdateAsync(set => set.SetProperty(item => item.PaymentsAfter, toVersion), cancellationToken),
                _ => 0
            };

            if (moved != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            _context.WebhookDeliveries.AddRange(deliveries);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // A delivery already queued (another round raced this one): nothing changes.
                await transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                _context.Forget();
                return false;
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    public void Add(OperationsWebhookDelivery delivery) => _context.WebhookDeliveries.Add(delivery ?? throw new ArgumentNullException(nameof(delivery)));

    public Task<OperationsWebhookDelivery?> GetDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken) =>
        _context.WebhookDeliveries.FirstOrDefaultAsync(delivery => delivery.Id == deliveryId, cancellationToken);

    public async Task<IReadOnlyList<OperationsWebhookDelivery>> ListDeliveriesAsync(int take, CancellationToken cancellationToken) =>
        await _context.WebhookDeliveries.AsNoTracking().OrderByDescending(delivery => delivery.CreatedUtc).Take(Math.Clamp(take, 1, 500)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> DueDeliveryIdsAsync(DateTimeOffset utcNow, int take, CancellationToken cancellationToken) =>
        await _context.WebhookDeliveries
            .Where(delivery => delivery.Status == OperationsWebhookStatus.Pending && delivery.NextAttemptUtc <= utcNow
                && (delivery.LeaseUntilUtc == null || delivery.LeaseUntilUtc <= utcNow))
            .OrderBy(delivery => delivery.NextAttemptUtc).ThenBy(delivery => delivery.CreatedUtc).Take(Math.Clamp(take, 1, 500)).Select(delivery => delivery.Id)
            .ToListAsync(cancellationToken);

    public async Task<OperationsWebhookDelivery?> ClaimAsync(Guid deliveryId, Guid token, DateTimeOffset until, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        int claimed = await _context.WebhookDeliveries
            .Where(delivery => delivery.Id == deliveryId && delivery.Status == OperationsWebhookStatus.Pending && delivery.NextAttemptUtc <= utcNow
                && (delivery.LeaseUntilUtc == null || delivery.LeaseUntilUtc <= utcNow))
            .ExecuteUpdateAsync(set => set.SetProperty(delivery => delivery.LeaseToken, token).SetProperty(delivery => delivery.LeaseUntilUtc, until), cancellationToken);
        return claimed == 1 ? await _context.WebhookDeliveries.AsNoTracking().FirstOrDefaultAsync(delivery => delivery.Id == deliveryId, cancellationToken) : null;
    }

    public async Task RecordAsync(OperationsWebhookDelivery delivery, Guid token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        await _context.WebhookDeliveries.Where(item => item.Id == delivery.Id && item.LeaseToken == token)
            .ExecuteUpdateAsync(set => set
                .SetProperty(item => item.Status, delivery.Status)
                .SetProperty(item => item.Attempts, delivery.Attempts)
                .SetProperty(item => item.NextAttemptUtc, delivery.NextAttemptUtc)
                .SetProperty(item => item.LastAttemptUtc, delivery.LastAttemptUtc)
                .SetProperty(item => item.LastStatusCode, delivery.LastStatusCode)
                .SetProperty(item => item.LastError, delivery.LastError)
                .SetProperty(item => item.LeaseUntilUtc, (DateTimeOffset?)null)
                .SetProperty(item => item.LeaseToken, (Guid?)null)
                .SetProperty(item => item.LastModifiedUtc, delivery.LastModifiedUtc), cancellationToken);
    }

    // At most once a minute while all is well, so staff edits of the address rarely meet a change.
    public Task EndpointDeliveredAsync(Guid endpointId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        DateTimeOffset aMinuteAgo = utcNow.AddMinutes(-1);
        return _context.WebhookEndpoints
            .Where(endpoint => endpoint.Id == endpointId && (endpoint.FailingSinceUtc != null || endpoint.LastSuccessUtc == null || endpoint.LastSuccessUtc < aMinuteAgo))
            .ExecuteUpdateAsync(set => set.SetProperty(endpoint => endpoint.LastSuccessUtc, utcNow).SetProperty(endpoint => endpoint.FailingSinceUtc, (DateTimeOffset?)null),
                cancellationToken);
    }

    public async Task<bool> EndpointFailedAsync(Guid endpointId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        await _context.WebhookEndpoints.Where(endpoint => endpoint.Id == endpointId && endpoint.FailingSinceUtc == null)
            .ExecuteUpdateAsync(set => set.SetProperty(endpoint => endpoint.FailingSinceUtc, utcNow), cancellationToken);
        DateTimeOffset since = utcNow - OperationsWebhookEndpoint.FailingLimit;
        return await _context.WebhookEndpoints.Where(endpoint => endpoint.Id == endpointId && endpoint.Active && endpoint.FailingSinceUtc <= since)
            .ExecuteUpdateAsync(set => set.SetProperty(endpoint => endpoint.Active, false).SetProperty(endpoint => endpoint.DisabledForFailures, true), cancellationToken) == 1;
    }

    public Task<int> PruneDeliveriesAsync(DateTimeOffset before, CancellationToken cancellationToken) =>
        _context.WebhookDeliveries.Where(delivery => delivery.Status != OperationsWebhookStatus.Pending && delivery.CreatedUtc < before).ExecuteDeleteAsync(cancellationToken);

    public Task<int> PruneEventsAsync(DateTimeOffset before, CancellationToken cancellationToken) =>
        _context.ApiEvents.Where(item => item.CreatedUtc < before).ExecuteDeleteAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> TenantsWithActiveEndpointsAsync(CancellationToken cancellationToken) =>
        await _context.WebhookEndpoints.Where(endpoint => endpoint.Active).Select(endpoint => endpoint.TenantId).Distinct().ToListAsync(cancellationToken);

    private Guid Tenant() => _scope.TenantId ?? throw new InvalidOperationException("Webhook events are read in a tenant scope (D-F1).");
}
