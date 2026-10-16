using Vixly.Contracts.Platform.Events;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.UseCases.Events.ReadPlatformEvents;

// =========================================================
// FILE: ReadPlatformEventsUseCase.cs
//
// PURPOSE:
// Public events after a cursor, oldest first, for Operations and Edge (Slice 9, D-K3).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called with the events scope; the API passes the types the caller may read (Edge: three)
// - At most 500 per call; More says to call again
// =========================================================
public sealed class ReadPlatformEventsUseCase
{
    private readonly IPlatformPublishedEventRepository _events;

    private readonly TimeProvider _timeProvider;

    public ReadPlatformEventsUseCase(
        IPlatformPublishedEventRepository events,
        TimeProvider timeProvider)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ReadPlatformEventsResponse> ExecuteAsync(
        PlatformInternalAuthorizationContext context,
        ReadPlatformEventsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        int limit = Math.Clamp(request.Limit, 1, MaxLimit);
        IReadOnlyCollection<string> types = request.Types.Count == 0 ? PlatformEventTypes.All.ToArray() : request.Types;

        // Rows newer than the settle time may still have earlier rows committing around them.
        IReadOnlyList<PlatformPublishedEvent> items = await _events.ListAfterAsync(
            Math.Max(0, request.After), limit + 1, _timeProvider.GetUtcNow() - SettleTime, types, cancellationToken);
        IReadOnlyList<PlatformPublishedEvent> page = items.Take(limit).ToArray();

        return ReadPlatformEventsResponse.Success(page, page.Count == 0 ? Math.Max(0, request.After) : page[^1].Sequence, items.Count > limit);
    }

    public const int MaxLimit = 500;

    public static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(5);
}
