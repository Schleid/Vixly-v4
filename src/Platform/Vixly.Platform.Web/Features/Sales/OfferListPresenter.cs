using System.Globalization;
using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Sales.ListPlatformOffers;
using Vixly.Platform.Domain.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: OfferListPresenter.cs
//
// PURPOSE:
// Pages the offer links.
//
// RESPONSIBILITIES:
// - Run the use cases for its component and hold the state
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// =========================================================
public sealed class OfferListPresenter : PlatformPresenter<OfferListState>
{
    private readonly ListPlatformOffersUseCase _list;

    public OfferListPresenter(ListPlatformOffersUseCase list, PlatformUseCaseScope scope, ILogger<OfferListPresenter> logger)
        : base(scope, OfferListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(PlatformOfferStatus? status, string? search, int page)
    {
        ListPlatformOffersResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(
                context,
                new ListPlatformOffersRequest { Status = status, Search = search, Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, Page = page },
                cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Offers is { } offers
            ? new OfferListState(offers.Items, offers.Page, offers.TotalPages, offers.TotalCount, response.Abilities, true, null)
            : State with { Loaded = true, Error = error });
    }
}
