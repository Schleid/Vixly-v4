using System.Globalization;
using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Subscriptions.ListPlatformSubscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: SubscriptionListPresenter.cs
//
// PURPOSE:
// Behaviour of the subscriptions list.
//
// RESPONSIBILITIES:
// - Load a page of subscriptions for the chosen filters
//
// IMPORTANT NOTES:
// - Ended subscriptions are hidden unless asked for
// =========================================================
public sealed class SubscriptionListPresenter : PlatformPresenter<SubscriptionListState>
{
    private readonly ListPlatformSubscriptionsUseCase _list;

    public SubscriptionListPresenter(ListPlatformSubscriptionsUseCase list, PlatformUseCaseScope scope, ILogger<SubscriptionListPresenter> logger)
        : base(scope, SubscriptionListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(PlatformSubscriptionStatus? status, Guid? planId, int? renewsWithinDays, string? search, bool includeEnded, bool discountedOnly, int page)
    {
        ListPlatformSubscriptionsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(
                context,
                new ListPlatformSubscriptionsRequest
                {
                    Status = status,
                    PlatformPlanId = planId,
                    RenewsWithinDays = renewsWithinDays,
                    Search = search,
                    IncludeEnded = includeEnded,
                    DiscountedOnly = discountedOnly,
                    Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
                    Page = page
                },
                cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Subscriptions is { } subscriptions
            ? new SubscriptionListState(subscriptions.Items, subscriptions.Page, subscriptions.TotalPages, subscriptions.TotalCount, true, null)
            : State with { Loaded = true, Error = error });
    }
}
