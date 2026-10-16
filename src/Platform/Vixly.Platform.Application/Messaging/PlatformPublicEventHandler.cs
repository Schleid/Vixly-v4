using System.Text.Json;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformPublicEventHandler.cs
//
// PURPOSE:
// Adds the public events of one outbox message type to the feed (Slice 9, D-K3).
//
// RESPONSIBILITIES:
// - Map the message and add each public event once
//
// IMPORTANT NOTES:
// - One instance per source type (registered in a loop)
// - A retried message finds its event already there and adds nothing
// =========================================================
public sealed class PlatformPublicEventHandler : IPlatformOutboxHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPlatformPublishedEventRepository _events;

    private readonly TimeProvider _timeProvider;

    public PlatformPublicEventHandler(string messageType, IPlatformPublishedEventRepository events, TimeProvider timeProvider)
    {
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public string MessageType { get; }

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        foreach (PlatformPublicEvent item in PlatformPublicEvents.From(message))
        {
            if (!await _events.ExistsAsync(message.Id, item.Type, cancellationToken))
            {
                _events.Add(PlatformPublishedEvent.Create(
                    item.Type,
                    item.TenantId,
                    message.Id,
                    JsonSerializer.Serialize(item.Data, item.Data.GetType(), JsonOptions),
                    item.OccurredUtc,
                    _timeProvider.GetUtcNow()));
            }
        }
    }
}
