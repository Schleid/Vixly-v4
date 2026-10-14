using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: IPlatformOutboxHandler.cs
//
// PURPOSE:
// Something that acts on one type of outbox message.
//
// RESPONSIBILITIES:
// - Name the message type it handles
// - Handle one message
//
// IMPORTANT NOTES:
// - Delivery is at least once: a handler must cope with seeing a
//   message again after a crash
// - Throw to report a failure; the dispatcher backs off
// =========================================================
public interface IPlatformOutboxHandler
{
    string MessageType { get; }

    Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken);
}
