using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Setup;
using Vixly.Operations.Domain.Jobs;
using Vixly.Operations.Domain.Platform;

namespace Vixly.Operations.Application.Platform;

// =========================================================
// FILE: OperationsPlatformFeedClock.cs
//
// PURPOSE:
// Reads Platform's event feed every minute and keeps Operations in step (Stage 3, Slice 0, D-F3).
//
// RESPONSIBILITIES:
// - Read from the stored position, page by page
// - Forget a tenant's cached answer when its status, address or subscription changes
// - Store the new position and a job run in one save
//
// IMPORTANT NOTES:
// - Runs in the system scope
// - Platform unreachable: the run is recorded as failed and the position does not move
// - Later slices add their reactions here (devices, seats, legal)
// =========================================================
public sealed class OperationsPlatformFeedClock
{
    public const string JobName = "ops.platform_feed";

    public const int PageSize = 200;

    public const int MaxPages = 10;

    private readonly IOperationsPlatformClient _platform;

    private readonly IOperationsTenantDirectory _directory;

    private readonly IOperationsFeedPositionRepository _positions;

    private readonly IOperationsJobRunRepository _runs;

    private readonly IOperationsTenantScope _scope;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly OperationsOutboxWriter _outbox;

    private readonly TimeProvider _timeProvider;

    public OperationsPlatformFeedClock(
        IOperationsPlatformClient platform,
        IOperationsTenantDirectory directory,
        IOperationsFeedPositionRepository positions,
        IOperationsJobRunRepository runs,
        IOperationsTenantScope scope,
        IOperationsUnitOfWork unitOfWork,
        OperationsOutboxWriter outbox,
        TimeProvider timeProvider)
    {
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _positions = positions ?? throw new ArgumentNullException(nameof(positions));
        _runs = runs ?? throw new ArgumentNullException(nameof(runs));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        _scope.EnterSystem();
        OperationsJobRun run = OperationsJobRun.Start(JobName, _timeProvider.GetUtcNow());
        _runs.Add(run);

        OperationsPlatformFeedPosition? position = await _positions.FindAsync(OperationsPlatformFeedPosition.PlatformEvents, cancellationToken);

        if (position is null)
        {
            position = OperationsPlatformFeedPosition.Create(OperationsPlatformFeedPosition.PlatformEvents, _timeProvider.GetUtcNow());
            _positions.Add(position);
        }

        int handled = 0;

        try
        {
            for (int page = 0; page < MaxPages; page++)
            {
                OperationsPlatformEventPage events = await _platform.ReadEventsAsync(position.Position, PageSize, cancellationToken);

                foreach (OperationsPlatformEvent item in events.Items)
                {
                    if (item.TenantId is { } tenantId && item.Type is OperationsPlatformEventTypes.TenantStatusChanged
                        or OperationsPlatformEventTypes.TenantSlugCorrected or OperationsPlatformEventTypes.SubscriptionStatusChanged)
                    {
                        _directory.Forget(tenantId);
                    }

                    // Slice 1: invite the owner in the tenant scope, through the outbox, so a failure retries.
                    if (item.TenantId is { } readyTenant && item.Type == OperationsPlatformEventTypes.RegistrationAccountSetupReady)
                    {
                        _outbox.Write(OperationsSetupMessages.OwnerInvitationRequested, readyTenant, readyTenant, new OperationsOwnerInvitationRequested(readyTenant));
                    }

                    handled++;
                }

                position.MoveTo(events.Next, _timeProvider.GetUtcNow());

                if (!events.More)
                {
                    break;
                }
            }

            run.Finish(handled, 0, null, _timeProvider.GetUtcNow());
        }
        catch (OperationsPlatformUnavailableException)
        {
            run.Finish(handled, 1, "ops.common.platform_unavailable", _timeProvider.GetUtcNow());
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return handled;
    }
}
