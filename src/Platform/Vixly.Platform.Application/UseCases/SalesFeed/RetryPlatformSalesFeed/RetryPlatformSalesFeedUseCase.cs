using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.SalesFeed;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.SalesFeed;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.SalesFeed.RetryPlatformSalesFeed;

// =========================================================
// FILE: RetryPlatformSalesFeedUseCase.cs
//
// PURPOSE:
// Starts the sales feed to TechOS again after a stop or a long retry.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// - Use after fixing the URL or the secret; the next run sends at once
// =========================================================
public sealed class RetryPlatformSalesFeedUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly IPlatformSalesFeedRepository _feed;

    private readonly PlatformSalesFeedSender _sender;

    public RetryPlatformSalesFeedUseCase(PlatformPaymentsAdministration payments, IPlatformSalesFeedRepository feed, PlatformSalesFeedSender sender)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public async Task<RetryPlatformSalesFeedResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RetryPlatformSalesFeedRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.PaymentsConfigure, requireStepUp: false, cancellationToken) is { } refused)
        {
            return RetryPlatformSalesFeedResponse.Failure(refused);
        }

        PlatformSalesFeedState state = await _sender.StateAsync(_payments.UtcNow, cancellationToken);
        string? problem = state.Problem;
        state.TryAgain(_payments.UtcNow);
        _payments.Audit(context, "platform.sales_feed.retried", PlatformPermissions.PaymentsConfigure, nameof(PlatformSalesFeedState), state.Id, new { problem });

        return await _payments.SaveAsync(cancellationToken)
            ? RetryPlatformSalesFeedResponse.Success()
            : RetryPlatformSalesFeedResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
