using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.SalesFeed;

namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedSender.cs
//
// PURPOSE:
// Posts pending sale events to TechOS in batches and acts on each answer (contract version 1).
//
// RESPONSIBILITIES:
// - Send up to 100 events per batch, oldest first, signed with the time of each attempt
// - 200: mark accepted and duplicates; park each rejected event and alert staff; send the rest normally
// - 413: halve the batch and try again
// - 400 or 401: a configuration problem; stop sending, keep every event, alert staff
// - 404, 5xx or no answer: retry with backoff; alert staff after 24 hours and keep retrying
//
// IMPORTANT NOTES:
// - Off while the URL or the secret is missing: events are still written and wait
// - A parked event never blocks the events after it
// - Each batch's answer is saved before the next batch is sent
// =========================================================
public sealed class PlatformSalesFeedSender
{
    // Batches per run, so one run stays short.
    public const int MaxBatchesPerRun = 20;

    private readonly IPlatformSalesFeedRepository _feed;

    private readonly IPlatformSalesFeedClient _client;

    private readonly PlatformOutboxWriter _outbox;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public PlatformSalesFeedSender(IPlatformSalesFeedRepository feed, IPlatformSalesFeedClient client, PlatformOutboxWriter outbox, IPlatformUnitOfWork unitOfWork)
    {
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PlatformSalesFeedState> StateAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        PlatformSalesFeedState? state = await _feed.GetStateAsync(cancellationToken);

        if (state is null)
        {
            state = PlatformSalesFeedState.Create(utcNow);
            _feed.AddState(state);
        }

        return state;
    }

    // Returns how many events TechOS answered for.
    public async Task<int> SendAsync(Func<DateTimeOffset> clock, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (!_client.Configured)
        {
            return 0;
        }

        PlatformSalesFeedState state = await StateAsync(clock(), cancellationToken);
        int size = PlatformSalesFeedLimits.MaxBatch;
        int answered = 0;

        for (int round = 0; round < MaxBatchesPerRun && state.Due(clock()); round++)
        {
            IReadOnlyList<PlatformSalesFeedEvent> batch = await _feed.ListPendingAsync(size, cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            PlatformSalesFeedAnswer answer = await _client.SendAsync(PlatformSalesFeedJson.Batch(batch.Select(item => item.Payload)), cancellationToken);
            DateTimeOffset utcNow = clock();
            bool keepGoing;

            switch (answer.Kind)
            {
                case PlatformSalesFeedAnswerKind.Read:
                    (int handled, bool complete) = Apply(state, batch, answer, utcNow);
                    answered += handled;
                    state.Succeeded(utcNow);
                    size = PlatformSalesFeedLimits.MaxBatch;

                    // An answer that left events out: they are sent again at the next run, never in a loop.
                    keepGoing = complete;
                    break;
                case PlatformSalesFeedAnswerKind.TooLarge when batch.Count > 1:
                    size = Math.Max(1, batch.Count / 2);
                    keepGoing = true;
                    break;
                case PlatformSalesFeedAnswerKind.TooLarge:
                    // One event alone is too large: it can never be taken; staff see it parked.
                    batch[0].Park(answer.Code, utcNow);
                    Alert(state, PlatformSalesFeedAlertKind.Parked, 1, answer.Code, utcNow);
                    answered++;
                    size = PlatformSalesFeedLimits.MaxBatch;
                    keepGoing = true;
                    break;
                case PlatformSalesFeedAnswerKind.Refused:
                    state.Stop(answer.Code, utcNow);
                    Alert(state, PlatformSalesFeedAlertKind.Stopped, await _feed.CountAsync(PlatformSalesFeedStatus.Pending, cancellationToken), answer.Code, utcNow);
                    keepGoing = false;
                    break;
                default:
                    if (state.Failed(answer.Code, utcNow))
                    {
                        Alert(state, PlatformSalesFeedAlertKind.NoAnswer, await _feed.CountAsync(PlatformSalesFeedStatus.Pending, cancellationToken), answer.Code, utcNow);
                    }

                    keepGoing = false;
                    break;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!keepGoing)
            {
                break;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return answered;
    }

    private (int Handled, bool Complete) Apply(PlatformSalesFeedState state, IReadOnlyList<PlatformSalesFeedEvent> batch, PlatformSalesFeedAnswer answer, DateTimeOffset utcNow)
    {
        HashSet<Guid> accepted = answer.Accepted.ToHashSet();
        HashSet<Guid> duplicates = answer.Duplicates.ToHashSet();
        Dictionary<Guid, string> rejected = answer.Rejected.GroupBy(item => item.Id).ToDictionary(group => group.Key, group => group.First().Error);
        int handled = 0;
        int parked = 0;

        foreach (PlatformSalesFeedEvent item in batch)
        {
            if (rejected.TryGetValue(item.Id, out string? error))
            {
                item.Park(error, utcNow);
                parked++;
            }
            else if (accepted.Contains(item.Id) || duplicates.Contains(item.Id))
            {
                item.Accept(!accepted.Contains(item.Id), utcNow);
            }
            else
            {
                continue;
            }

            handled++;
        }

        if (parked > 0)
        {
            Alert(state, PlatformSalesFeedAlertKind.Parked, parked, answer.Code, utcNow);
        }

        return (handled, handled == batch.Count);
    }

    // The feed's state row is the alerts' outbox stream.
    private void Alert(PlatformSalesFeedState state, PlatformSalesFeedAlertKind kind, int count, string code, DateTimeOffset utcNow)
    {
        _outbox.Write(PlatformMessageTypes.SalesFeedAlert, state.Id, new PlatformSalesFeedAlert(kind, count, code, utcNow));
    }
}
