using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.SalesFeed;

namespace Vixly.Platform.Application.UseCases.SalesFeed.GetPlatformSalesFeed;

// =========================================================
// FILE: GetPlatformSalesFeedUseCase.cs
//
// PURPOSE:
// Shows how the sales feed to TechOS stands: pending, parked with TechOS's error, last success.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then read
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// - Read by billing staff; acting needs the payments settings permission
// =========================================================
public sealed class GetPlatformSalesFeedUseCase
{
    // Parked events listed on the page.
    public const int ParkedShown = 100;

    private readonly PlatformPaymentsAdministration _payments;

    private readonly IPlatformSalesFeedRepository _feed;

    private readonly IPlatformSalesFeedClient _client;

    public GetPlatformSalesFeedUseCase(PlatformPaymentsAdministration payments, IPlatformSalesFeedRepository feed, IPlatformSalesFeedClient client)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<GetPlatformSalesFeedResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformSalesFeedRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformSalesFeedResponse.Failure(refused);
        }

        PlatformSalesFeedState? state = await _feed.GetStateAsync(cancellationToken);
        IReadOnlyList<PlatformSalesFeedEvent> parked = await _feed.ListParkedAsync(ParkedShown, cancellationToken);

        return GetPlatformSalesFeedResponse.Success(
            new PlatformSalesFeedOverviewItem(
                _client.Configured,
                _client.SettingsProblem,
                state?.Problem,
                state?.ProblemUtc,
                state?.LastSuccessUtc,
                state?.FailingSinceUtc,
                state?.LastFailure,
                state?.NextAttemptUtc,
                await _feed.CountAsync(PlatformSalesFeedStatus.Pending, cancellationToken),
                await _feed.CountAsync(PlatformSalesFeedStatus.Parked, cancellationToken),
                await _feed.CountAsync(PlatformSalesFeedStatus.Accepted, cancellationToken) + await _feed.CountAsync(PlatformSalesFeedStatus.Duplicate, cancellationToken),
                parked.Select(item => new PlatformSalesFeedParkedItem(item.Id, item.Sequence, item.EventType, item.OccurredUtc, item.PlatformTenantId, item.LastError, item.SentUtc)).ToArray()),
            await _payments.HoldsAsync(context, PlatformPermissions.PaymentsConfigure, cancellationToken));
    }
}
