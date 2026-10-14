using System.Globalization;
using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Sales.ListPlatformPromotions;
using Vixly.Platform.Domain.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: PromotionListPresenter.cs
//
// PURPOSE:
// Pages the promotions.
//
// RESPONSIBILITIES:
// - Run the use cases for its component and hold the state
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// =========================================================
public sealed class PromotionListPresenter : PlatformPresenter<PromotionListState>
{
    private readonly ListPlatformPromotionsUseCase _list;

    public PromotionListPresenter(ListPlatformPromotionsUseCase list, PlatformUseCaseScope scope, ILogger<PromotionListPresenter> logger)
        : base(scope, PromotionListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(PlatformPromotionStatus? status, string? search, int page)
    {
        ListPlatformPromotionsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(
                context,
                new ListPlatformPromotionsRequest { Status = status, Search = search, Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, Page = page },
                cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Promotions is { } promotions
            ? new PromotionListState(promotions.Items, promotions.Page, promotions.TotalPages, promotions.TotalCount, response.Abilities, true, null)
            : State with { Loaded = true, Error = error });
    }
}
