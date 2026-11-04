using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Messaging;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.Messaging;

// =========================================================
// FILE: OperationsOutboxDispatcher.cs
//
// PURPOSE:
// Delivers due outbox messages to their handlers (as Platform).
//
// RESPONSIBILITIES:
// - Lease a batch of due messages (in the system scope)
// - Handle one message in its own scope, entering its tenant first
//
// IMPORTANT NOTES:
// - The host creates a new scope for each message, so a message for tenant A never runs in tenant B's scope
// - A failure backs off; after 10 attempts the message waits for a person
// =========================================================
public sealed class OperationsOutboxDispatcher
{
    public const int BatchSize = 20;

    public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    private const string HandlerFailed = "ops.messaging.handler_failed";

    private readonly IOperationsOutboxRepository _outbox;

    private readonly IEnumerable<IOperationsOutboxHandler> _handlers;

    private readonly IOperationsTenantScope _scope;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OperationsOutboxDispatcher(
        IOperationsOutboxRepository outbox,
        IEnumerable<IOperationsOutboxHandler> handlers,
        IOperationsTenantScope scope,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    // =====================================================
    // METHOD: LEASE DUE MESSAGES
    // =====================================================
    public Task<IReadOnlyList<Guid>> LeaseAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        return _outbox.LeaseDueAsync(BatchSize, utcNow, utcNow.Add(LeaseDuration), cancellationToken);
    }

    // =====================================================
    // METHOD: HANDLE ONE MESSAGE
    // =====================================================
    // Call in a fresh scope per message. Returns true when the message was delivered.
    public async Task<bool> HandleAsync(Guid messageId, Action<OperationsOutboxFailure>? onFailure, CancellationToken cancellationToken)
    {
        OperationsOutboxMessage? message = await _outbox.GetAsync(messageId, cancellationToken);

        if (message is null || message.ProcessedUtc is not null)
        {
            return false;
        }

        if (message.TenantId is { } tenantId)
        {
            _scope.Enter(tenantId);
        }
        else
        {
            _scope.EnterSystem();
        }

        bool delivered = false;

        try
        {
            foreach (IOperationsOutboxHandler handler in _handlers.Where(handler => handler.MessageType == message.Type))
            {
                await handler.HandleAsync(message, cancellationToken);
            }

            message.MarkProcessed(_timeProvider.GetUtcNow());
            delivered = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            message = await _outbox.GetAsync(messageId, cancellationToken) ?? message;
            message.MarkFailed(exception.Error.Code, _timeProvider.GetUtcNow());
            onFailure?.Invoke(new OperationsOutboxFailure(message.Id, message.Type, exception.Error.Code, message.Attempts, exception));
        }
        catch (Exception exception)
        {
            _unitOfWork.DiscardChanges();
            message = await _outbox.GetAsync(messageId, cancellationToken) ?? message;
            message.MarkFailed(HandlerFailed, _timeProvider.GetUtcNow());
            onFailure?.Invoke(new OperationsOutboxFailure(message.Id, message.Type, HandlerFailed, message.Attempts, exception));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return delivered;
    }
}
