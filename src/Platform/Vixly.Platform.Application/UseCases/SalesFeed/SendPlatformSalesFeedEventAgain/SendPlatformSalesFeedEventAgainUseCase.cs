using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.SalesFeed;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.SalesFeed;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.SalesFeed.SendPlatformSalesFeedEventAgain;

// =========================================================
// FILE: SendPlatformSalesFeedEventAgainUseCase.cs
//
// PURPOSE:
// Sends a parked sale event to TechOS again, unchanged.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// - The event keeps its id and its JSON: TechOS stores it once whatever happens
// =========================================================
public sealed class SendPlatformSalesFeedEventAgainUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly IPlatformSalesFeedRepository _feed;

    private readonly PlatformSalesFeedSender _sender;

    public SendPlatformSalesFeedEventAgainUseCase(PlatformPaymentsAdministration payments, IPlatformSalesFeedRepository feed, PlatformSalesFeedSender sender)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public async Task<SendPlatformSalesFeedEventAgainResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SendPlatformSalesFeedEventAgainRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.PaymentsConfigure, requireStepUp: false, cancellationToken) is { } refused)
        {
            return SendPlatformSalesFeedEventAgainResponse.Failure(refused);
        }

        PlatformSalesFeedEvent? feedEvent = await _feed.GetAsync(request.EventId, cancellationToken);

        if (feedEvent is null)
        {
            return SendPlatformSalesFeedEventAgainResponse.Failure(PlatformSalesFeedErrors.EventNotFound);
        }

        try
        {
            feedEvent.SendAgain(_payments.UtcNow);
        }
        catch (DomainException exception)
        {
            return SendPlatformSalesFeedEventAgainResponse.Failure(exception.Error);
        }

        // A stopped feed starts again too: staff fixed what TechOS refused.
        (await _sender.StateAsync(_payments.UtcNow, cancellationToken)).TryAgain(_payments.UtcNow);
        _payments.Audit(context, "platform.sales_feed.event_sent_again", PlatformPermissions.PaymentsConfigure, nameof(PlatformSalesFeedEvent), feedEvent.Id,
            new { type = feedEvent.EventType, sequence = feedEvent.Sequence });

        return await _payments.SaveAsync(cancellationToken)
            ? SendPlatformSalesFeedEventAgainResponse.Success()
            : SendPlatformSalesFeedEventAgainResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
