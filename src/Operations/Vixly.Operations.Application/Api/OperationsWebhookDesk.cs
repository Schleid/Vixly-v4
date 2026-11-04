using System.Text.Json;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Api;

namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsWebhookDesk.cs
//
// PURPOSE:
// Turns a company’s events into signed webhook deliveries and sends them (Slice 10).
//
// RESPONSIBILITIES:
// - Read the events after each address’s place, per kind, and queue one delivery per event with the record as the API shows it
// - Send the deliveries due, each claimed just before it goes; record the result; turn off an address failing for three days
// - Send a test event at once
//
// IMPORTANT NOTES:
// - Run per company under its tenant scope; a time budget per round keeps one slow receiver from holding up others
// - Events are numbered by the database as they are saved and read only up to the oldest save in progress: none is skipped (review)
// - Without the plan feature nothing is queued or sent and the places move on (no flood when it comes back)
// =========================================================
public sealed class OperationsWebhookDesk
{
    public static readonly TimeSpan Keep = TimeSpan.FromDays(30);

    public static readonly TimeSpan LeaseFor = TimeSpan.FromSeconds(60);

    public const int BatchSize = 100;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IOperationsApiRepository _api;

    private readonly IOperationsApiReader _reader;

    private readonly IOperationsWebhookSender _sender;

    private readonly IOperationsSecretProtector _protector;

    private readonly IOperationsPlanFeatures _features;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OperationsWebhookDesk(IOperationsApiRepository api, IOperationsApiReader reader, IOperationsWebhookSender sender, IOperationsSecretProtector protector,
        IOperationsPlanFeatures features, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public IOperationsApiRepository Store => _api;

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public static OperationsApiResource ResourceOf(OperationsWebhookEvents kind) => kind switch
    {
        OperationsWebhookEvents.Tracking => OperationsApiResource.Tracking,
        OperationsWebhookEvents.CustomerCreated => OperationsApiResource.Customers,
        OperationsWebhookEvents.InvoiceIssued => OperationsApiResource.Invoices,
        OperationsWebhookEvents.PaymentRecorded => OperationsApiResource.Payments,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    // The body every receiver gets: the event id, its name, when, and the record as the API shows it.
    public static string Body(string eventId, string eventType, DateTimeOffset createdUtc, object data) =>
        JsonSerializer.Serialize(new { id = eventId, type = eventType, createdUtc, data }, Json);

    // One round for the company in scope: queue what happened, then send what is due within the budget. The number of deliveries tried.
    public async Task<int> RunAsync(Guid tenantId, TimeSpan budget, CancellationToken cancellationToken)
    {
        bool inPlan = (await _features.GetAsync(tenantId, cancellationToken)).TenantApi;
        await QueueAsync(tenantId, inPlan, cancellationToken);
        int sent = inPlan ? await DeliverDueAsync(UtcNow + budget, cancellationToken) : 0;
        await _api.PruneDeliveriesAsync(UtcNow - Keep, cancellationToken);
        return sent;
    }

    public async Task QueueAsync(Guid tenantId, bool inPlan, CancellationToken cancellationToken)
    {
        foreach (OperationsWebhookEndpoint endpoint in (await _api.ListEndpointsAsync(cancellationToken)).Where(endpoint => endpoint.Active))
        {
            foreach (OperationsWebhookEvents kind in OperationsWebhookEventNames.All.Where(kind => endpoint.Events.HasFlag(kind)))
            {
                long from = endpoint.Cursor(kind);
                IReadOnlyList<OperationsApiEventRow> events = await _api.EventsAfterAsync(kind, from, BatchSize, cancellationToken);

                if (events.Count == 0)
                {
                    continue;
                }

                List<OperationsWebhookDelivery> deliveries = new();

                // Out of plan: the place moves on, nothing is queued.
                if (inPlan)
                {
                    IReadOnlyDictionary<Guid, object> records = await _reader.GetManyAsync(ResourceOf(kind), events.Select(item => item.SourceId).Distinct().ToList(), cancellationToken);
                    string name = OperationsWebhookEventNames.Of(kind);
                    DateTimeOffset utcNow = UtcNow;

                    foreach (OperationsApiEventRow item in events)
                    {
                        // A record gone meanwhile (a voided tracking event stays; a removed row does not): nothing to send.
                        if (records.TryGetValue(item.SourceId, out object? record))
                        {
                            string eventId = "evt_" + item.Id.ToString("N");
                            deliveries.Add(OperationsWebhookDelivery.Create(tenantId, endpoint.Id, eventId, name, Body(eventId, name, item.CreatedUtc, record), utcNow));
                        }
                    }
                }

                // Another host moved the place first: its deliveries stand, these are dropped.
                if (!await _api.QueueAsync(endpoint.Id, kind, from, events[^1].Version, deliveries, cancellationToken))
                {
                    break;
                }
            }
        }
    }

    public async Task<int> DeliverDueAsync(DateTimeOffset stopAt, CancellationToken cancellationToken)
    {
        int tried = 0;

        foreach (Guid deliveryId in await _api.DueDeliveryIdsAsync(UtcNow, 200, cancellationToken))
        {
            if (UtcNow >= stopAt)
            {
                break;
            }

            if (await SendAsync(deliveryId, cancellationToken))
            {
                tried++;
            }
        }

        return tried;
    }

    // Claims the delivery, sends it, records the result under the claim. False when another sender has it.
    public async Task<bool> SendAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        Guid token = Guid.NewGuid();
        DateTimeOffset claimedUtc = UtcNow;

        if (await _api.ClaimAsync(deliveryId, token, claimedUtc + LeaseFor, claimedUtc, cancellationToken) is not { } delivery)
        {
            return false;
        }

        OperationsWebhookEndpoint? endpoint = await _api.GetEndpointAsync(delivery.EndpointId, cancellationToken);
        OperationsWebhookResult result;

        if (endpoint is null || !endpoint.Active)
        {
            result = new OperationsWebhookResult(null, "endpoint_off");
        }
        else if (_protector.Unprotect(endpoint.SecretProtected, OperationsSecretPurposes.WebhookSecret) is not { } secret)
        {
            result = new OperationsWebhookResult(null, "secret_unreadable");
        }
        else
        {
            long timestamp = UtcNow.ToUnixTimeSeconds();
            Dictionary<string, string> headers = new(StringComparer.Ordinal)
            {
                ["webhook-id"] = delivery.EventId,
                ["webhook-timestamp"] = timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["webhook-signature"] = OperationsWebhookSignature.Sign(secret, delivery.EventId, timestamp, delivery.Payload)
            };
            result = await _sender.PostAsync(endpoint.Url, headers, delivery.Payload, cancellationToken);
        }

        DateTimeOffset utcNow = UtcNow;

        if (result.Delivered)
        {
            delivery.Succeeded(result.StatusCode!.Value, utcNow);
            await _api.RecordAsync(delivery, token, cancellationToken);
            await _api.EndpointDeliveredAsync(delivery.EndpointId, utcNow, cancellationToken);
            return true;
        }

        delivery.Attempted(result.StatusCode, result.Error, utcNow);
        await _api.RecordAsync(delivery, token, cancellationToken);

        // A test never counts against the address; three days of real failures turn it off, on the record.
        if (!delivery.IsTest && endpoint is { Active: true } && await _api.EndpointFailedAsync(endpoint.Id, utcNow, cancellationToken))
        {
            _audit.Record(null, "ops.api.webhook_disabled", nameof(OperationsWebhookEndpoint), endpoint.Id, after: new { endpoint.Url });

            if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                _unitOfWork.DiscardChanges();
            }
        }

        return true;
    }
}
