using System.Text.Json;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformOutboxWriter.cs
//
// PURPOSE:
// Writes a message to the outbox in the current unit of work.
//
// RESPONSIBILITIES:
// - Serialise the payload with web defaults
// - Add the message so it commits with the change (D-2, D-T17)
//
// IMPORTANT NOTES:
// - Never sends anything itself: the dispatcher does, after commit
// =========================================================
public sealed class PlatformOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPlatformOutboxRepository _outbox;

    private readonly TimeProvider _timeProvider;

    public PlatformOutboxWriter(IPlatformOutboxRepository outbox, TimeProvider timeProvider)
    {
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public static T Read<T>(PlatformOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return JsonSerializer.Deserialize<T>(message.Payload, JsonOptions)
            ?? throw new InvalidOperationException("Empty outbox payload.");
    }

    public void Write<T>(string type, Guid streamId, T payload)
    {
        _outbox.Add(PlatformOutboxMessage.Create(
            type,
            streamId,
            JsonSerializer.Serialize(payload, JsonOptions),
            _timeProvider.GetUtcNow()));
    }
}
