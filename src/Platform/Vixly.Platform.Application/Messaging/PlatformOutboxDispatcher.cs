using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Messaging;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformOutboxDispatcher.cs
//
// PURPOSE:
// Delivers due outbox messages to their handlers (D-T17).
//
// RESPONSIBILITIES:
// - Lease a batch of due messages
// - Run every handler of each message
// - Mark it processed, or failed with back-off
//
// IMPORTANT NOTES:
// - Runs in Platform.Web until the Worker opens (Slice 4)
// - A message with no handler is marked processed: nothing waits
//   for it
// - Each message is saved on its own, so one failure does not
//   hold back the others
// - Each failure is reported to the caller with the message id
//   (onFailure), so the host can log it; Application has no logger
// =========================================================
public sealed class PlatformOutboxDispatcher
{
    public const int BatchSize = 20;

    public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    private const string HandlerFailed = "platform.messaging.handler_failed";

    private readonly IPlatformOutboxRepository _outbox;

    private readonly IEnumerable<IPlatformOutboxHandler> _handlers;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlatformOutboxDispatcher(
        IPlatformOutboxRepository outbox,
        IEnumerable<IPlatformOutboxHandler> handlers,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task<int> DispatchAsync(CancellationToken cancellationToken)
    {
        return DispatchAsync(null, cancellationToken);
    }

    public async Task<int> DispatchAsync(Action<PlatformOutboxFailure>? onFailure, CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        IReadOnlyList<Guid> leased = await _outbox.LeaseDueAsync(BatchSize, utcNow, utcNow.Add(LeaseDuration), cancellationToken);
        int handled = 0;

        foreach (Guid messageId in leased)
        {
            PlatformOutboxMessage? message = await _outbox.GetAsync(messageId, cancellationToken);

            if (message is null || message.ProcessedUtc is not null)
            {
                continue;
            }

            try
            {
                foreach (IPlatformOutboxHandler handler in _handlers.Where(handler => handler.MessageType == message.Type))
                {
                    await handler.HandleAsync(message, cancellationToken);
                }

                message.MarkProcessed(_timeProvider.GetUtcNow());
                handled++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (DomainException exception)
            {
                message.MarkFailed(exception.Error.Code, _timeProvider.GetUtcNow());
                onFailure?.Invoke(new PlatformOutboxFailure(message.Id, message.Type, exception.Error.Code, message.Attempts, exception));
            }
            catch (Exception exception)
            {
                message.MarkFailed(HandlerFailed, _timeProvider.GetUtcNow());
                onFailure?.Invoke(new PlatformOutboxFailure(message.Id, message.Type, HandlerFailed, message.Attempts, exception));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return handled;
    }
}
