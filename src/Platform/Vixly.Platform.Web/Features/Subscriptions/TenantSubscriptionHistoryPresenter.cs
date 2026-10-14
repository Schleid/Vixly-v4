using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Subscriptions.ListPlatformSubscriptionEvents;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantSubscriptionHistoryPresenter.cs
//
// PURPOSE:
// Behaviour of a tenant's subscription history.
//
// RESPONSIBILITIES:
// - Page through the history, newest first
//
// IMPORTANT NOTES:
// - History is append-only
// =========================================================
public sealed class TenantSubscriptionHistoryPresenter : PlatformPresenter<TenantSubscriptionHistoryState>
{
    private readonly ListPlatformSubscriptionEventsUseCase _list;

    public TenantSubscriptionHistoryPresenter(
        ListPlatformSubscriptionEventsUseCase list,
        PlatformUseCaseScope scope,
        ILogger<TenantSubscriptionHistoryPresenter> logger)
        : base(scope, TenantSubscriptionHistoryState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(Guid platformTenantId, int page)
    {
        ListPlatformSubscriptionEventsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(
                context,
                new ListPlatformSubscriptionEventsRequest { PlatformTenantId = platformTenantId, Page = page, PageSize = 10 },
                cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Events is { } events
            ? new TenantSubscriptionHistoryState(events.Items, events.Page, events.TotalPages, true, null)
            : State with { Loaded = true, Error = error });
    }
}
