using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.SalesFeed.GetPlatformSalesFeed;
using Vixly.Platform.Application.UseCases.SalesFeed.RebuildPlatformSalesFeedEvent;
using Vixly.Platform.Application.UseCases.SalesFeed.RetryPlatformSalesFeed;
using Vixly.Platform.Application.UseCases.SalesFeed.SendPlatformSalesFeedEventAgain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: SalesFeedPresenter.cs
//
// PURPOSE:
// Behaviour of the sales feed page: how the feed of Vixly sales to TechOS stands (2026-11-02).
//
// RESPONSIBILITIES:
// - Load the overview: pending, parked with TechOS's error, last success, retries, stop
// - Send a parked event again; start the feed again after a stop
//
// IMPORTANT NOTES:
// - Reading needs billing view; acting needs the payments settings permission
// =========================================================
public sealed class SalesFeedPresenter : PlatformPresenter<SalesFeedState>
{
    private readonly GetPlatformSalesFeedUseCase _get;

    private readonly SendPlatformSalesFeedEventAgainUseCase _sendAgain;

    private readonly RetryPlatformSalesFeedUseCase _retry;

    private readonly RebuildPlatformSalesFeedEventUseCase _rebuild;

    public SalesFeedPresenter(
        GetPlatformSalesFeedUseCase get,
        SendPlatformSalesFeedEventAgainUseCase sendAgain,
        RetryPlatformSalesFeedUseCase retry,
        RebuildPlatformSalesFeedEventUseCase rebuild,
        PlatformUseCaseScope scope,
        ILogger<SalesFeedPresenter> logger)
        : base(scope, SalesFeedState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _sendAgain = sendAgain ?? throw new ArgumentNullException(nameof(sendAgain));
        _retry = retry ?? throw new ArgumentNullException(nameof(retry));
        _rebuild = rebuild ?? throw new ArgumentNullException(nameof(rebuild));
    }

    public async Task LoadAsync()
    {
        GetPlatformSalesFeedResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformSalesFeedRequest(), cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Feed is { } feed
            ? State with { Feed = feed, CanManage = response.CanManage, Loaded = true }
            : State with { Loaded = true, Error = error });
    }

    public async Task SendAgainAsync(Guid eventId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
            (await _sendAgain.ExecuteAsync(context, new SendPlatformSalesFeedEventAgainRequest { EventId = eventId }, cancellationToken)).Error);
        SetState(State with { DoneKey = error is null ? "ui.sales_feed.sent_again" : null, Error = error });
        await LoadAsync();
    }

    // Writes a rejected event again from Vixly's history, under a new id, and replaces the rejected one.
    public async Task RebuildAsync(Guid eventId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
            (await _rebuild.ExecuteAsync(context, new RebuildPlatformSalesFeedEventRequest { EventId = eventId }, cancellationToken)).Error);
        SetState(State with { DoneKey = error is null ? "ui.sales_feed.rebuilt" : null, Error = error });
        await LoadAsync();
    }

    public async Task RetryAsync()
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
            (await _retry.ExecuteAsync(context, new RetryPlatformSalesFeedRequest(), cancellationToken)).Error);
        SetState(State with { DoneKey = error is null ? "ui.sales_feed.retried" : null, Error = error });
        await LoadAsync();
    }
}
