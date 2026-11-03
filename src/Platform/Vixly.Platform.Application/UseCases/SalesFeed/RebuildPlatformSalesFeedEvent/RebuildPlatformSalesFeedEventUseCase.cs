using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.SalesFeed;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.SalesFeed;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.SalesFeed.RebuildPlatformSalesFeedEvent;

// =========================================================
// FILE: RebuildPlatformSalesFeedEventUseCase.cs
//
// PURPOSE:
// Replaces a rejected sale event with a corrected one, written again from Vixly's history (TechOS, 2026-11-03).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Write the corrected event under a new id and the next sequence; mark the rejected one Replaced
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// - The rejected event stays on record unchanged; TechOS keys invoices by number, so the correction never counts twice
// - Daily totals leave out replaced events
// =========================================================
public sealed class RebuildPlatformSalesFeedEventUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly IPlatformSalesFeedRepository _feed;

    private readonly PlatformSalesFeedRecorder _recorder;

    private readonly PlatformSalesFeedSender _sender;

    public RebuildPlatformSalesFeedEventUseCase(PlatformPaymentsAdministration payments, IPlatformSalesFeedRepository feed, PlatformSalesFeedRecorder recorder, PlatformSalesFeedSender sender)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public async Task<RebuildPlatformSalesFeedEventResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RebuildPlatformSalesFeedEventRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.PaymentsConfigure, requireStepUp: false, cancellationToken) is { } refused)
        {
            return RebuildPlatformSalesFeedEventResponse.Failure(refused);
        }

        PlatformSalesFeedEvent? rejected = await _feed.GetAsync(request.EventId, cancellationToken);

        if (rejected is null)
        {
            return RebuildPlatformSalesFeedEventResponse.Failure(PlatformSalesFeedErrors.EventNotFound);
        }

        if (rejected.Status != PlatformSalesFeedStatus.Parked)
        {
            return RebuildPlatformSalesFeedEventResponse.Failure(PlatformSalesFeedErrors.NotParked);
        }

        PlatformSalesFeedEvent? corrected = await _recorder.RebuildAsync(rejected, _payments.UtcNow, cancellationToken);

        if (corrected is null)
        {
            return RebuildPlatformSalesFeedEventResponse.Failure(PlatformSalesFeedErrors.RebuildNotPossible);
        }

        try
        {
            rejected.Replace(_payments.UtcNow);
        }
        catch (DomainException exception)
        {
            return RebuildPlatformSalesFeedEventResponse.Failure(exception.Error);
        }

        _feed.Add(corrected);

        // A stopped feed starts again too: staff fixed what TechOS refused.
        (await _sender.StateAsync(_payments.UtcNow, cancellationToken)).TryAgain(_payments.UtcNow);
        _payments.Audit(context, "platform.sales_feed.event_rebuilt", PlatformPermissions.PaymentsConfigure, nameof(PlatformSalesFeedEvent), rejected.Id,
            new { type = rejected.EventType, sequence = rejected.Sequence, correctedSequence = corrected.Sequence, correctedId = corrected.Id });

        return await _payments.SaveAsync(cancellationToken)
            ? RebuildPlatformSalesFeedEventResponse.Success()
            : RebuildPlatformSalesFeedEventResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
