using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Messaging;

// =========================================================
// FILE: IOperationsOutboxHandler.cs
//
// PURPOSE:
// Handles one type of outbox message.
//
// RESPONSIBILITIES:
// - Do the work a message asks for
//
// IMPORTANT NOTES:
// - Runs in the tenant scope of the message when it names a tenant
// - Throws to retry; a DomainException code is recorded
// =========================================================
public interface IOperationsOutboxHandler
{
    string MessageType { get; }

    Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken);
}
