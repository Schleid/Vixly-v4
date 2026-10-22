using System.Text.Json;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Messaging;

// =========================================================
// FILE: OperationsOutboxWriter.cs
//
// PURPOSE:
// Writes outbox messages in the same unit of work as the change (D-2).
//
// RESPONSIBILITIES:
// - Serialize a payload into a message
// - Read a payload back
//
// IMPORTANT NOTES:
// - Payloads are JSON in web casing
// - The message records the tenant it concerns
// =========================================================
public sealed class OperationsOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOperationsOutboxRepository _outbox;

    private readonly TimeProvider _timeProvider;

    public OperationsOutboxWriter(IOperationsOutboxRepository outbox, TimeProvider timeProvider)
    {
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public static T Read<T>(OperationsOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return JsonSerializer.Deserialize<T>(message.Payload, JsonOptions)
            ?? throw new InvalidOperationException("Empty outbox payload.");
    }

    public void Write<T>(string type, Guid? tenantId, Guid streamId, T payload)
    {
        _outbox.Add(OperationsOutboxMessage.Create(type, tenantId, streamId, JsonSerializer.Serialize(payload, JsonOptions), _timeProvider.GetUtcNow()));
    }
}
