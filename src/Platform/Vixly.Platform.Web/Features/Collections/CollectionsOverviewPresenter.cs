using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Collections.GetPlatformCollectionsDashboard;
using Vixly.Platform.Application.UseCases.Collections.ListPlatformCollectionCases;
using Vixly.Platform.Domain.Collections;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Collections;

// =========================================================
// FILE: CollectionsOverviewPresenter.cs
//
// PURPOSE:
// Behaviour of the Collections page (D-C13, D-C14).
//
// RESPONSIBILITIES:
// - Load the dashboard and a page of cases for the chosen filters
//
// IMPORTANT NOTES:
// - Filtering and paging happen on the server
// =========================================================
public sealed class CollectionsOverviewPresenter : PlatformPresenter<CollectionsOverviewState>
{
    private readonly GetPlatformCollectionsDashboardUseCase _dashboard;

    private readonly ListPlatformCollectionCasesUseCase _list;

    public CollectionsOverviewPresenter(GetPlatformCollectionsDashboardUseCase dashboard, ListPlatformCollectionCasesUseCase list, PlatformUseCaseScope scope, ILogger<CollectionsOverviewPresenter> logger)
        : base(scope, CollectionsOverviewState.Initial, logger)
    {
        _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(PlatformCollectionCaseStatus? status, PlatformCollectionStage? stage, string? currency, bool? inExtension, string? search, int page)
    {
        GetPlatformCollectionsDashboardResponse? dashboard = null;
        ListPlatformCollectionCasesResponse? list = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            dashboard = await _dashboard.ExecuteAsync(context, new GetPlatformCollectionsDashboardRequest(), cancellationToken);

            if (dashboard.Error is not null)
            {
                return dashboard.Error;
            }

            list = await _list.ExecuteAsync(context, new ListPlatformCollectionCasesRequest
            {
                Status = status, Stage = stage, CurrencyCode = currency, InExtension = inExtension, Search = search, Page = page
            }, cancellationToken);
            return list.Error;
        });

        SetState(error is null && list?.Cases is { } result
            ? new CollectionsOverviewState(dashboard!.Dashboard, result.Items, result.Page, result.TotalPages, result.TotalCount, true, null)
            : State with { Loaded = true, Error = error });
    }
}
