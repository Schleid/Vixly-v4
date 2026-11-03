using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedClock.cs
//
// PURPOSE:
// One run of the sales feed job (every minute): write new sale events, the closed days' totals, then send.
//
// RESPONSIBILITIES:
// - Write the events of new history rows and save them
// - Write the daily totals once every history row has been written
// - Send pending events when the feed is configured
//
// IMPORTANT NOTES:
// - Runs in its own scope; a failure shows on the Jobs page and the next run starts again
// - Events are written even while the feed is off (no URL or secret)
// =========================================================
public sealed class PlatformSalesFeedClock
{
    private readonly PlatformSalesFeedRecorder _recorder;

    private readonly PlatformSalesFeedSender _sender;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlatformSalesFeedClock(PlatformSalesFeedRecorder recorder, PlatformSalesFeedSender sender, IPlatformUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        (int written, bool more) = await _recorder.RecordAsync(_timeProvider.GetUtcNow(), cancellationToken);

        if (written > 0)
        {
            await SaveAsync(cancellationToken);
        }

        if (!more)
        {
            written += await _recorder.TotalsAsync(_timeProvider.GetUtcNow(), cancellationToken);
            await SaveAsync(cancellationToken);
        }

        return written + await _sender.SendAsync(_timeProvider.GetUtcNow, cancellationToken);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        // Two runs at once would take the same sequence: the unique index refuses the second, which tries again next run.
        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            throw new InvalidOperationException("The sales feed events could not be saved; the next run tries again.");
        }
    }
}
