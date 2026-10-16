using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformEventFeedClock.cs
//
// PURPOSE:
// Removes public events past their 30 days (Slice 9, D-K3).
//
// RESPONSIBILITIES:
// - Delete old feed rows
//
// IMPORTANT NOTES:
// - Run by the Platform worker with the other clocks
// =========================================================
public sealed class PlatformEventFeedClock
{
    private readonly IPlatformPublishedEventRepository _events;

    private readonly TimeProvider _timeProvider;

    public PlatformEventFeedClock(IPlatformPublishedEventRepository events, TimeProvider timeProvider)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task<int> RunAsync(CancellationToken cancellationToken)
    {
        return _events.DeleteCreatedBeforeAsync(_timeProvider.GetUtcNow() - PlatformPublishedEvent.Retention, cancellationToken);
    }
}
