using System.Globalization;
using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Plans.ListPlatformPlans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Plans;

// =========================================================
// FILE: PlanListPresenter.cs
//
// PURPOSE:
// Behaviour of the plans list.
//
// RESPONSIBILITIES:
// - Load a page of plans for a status and a search
//
// IMPORTANT NOTES:
// - Names come in the reader language
// =========================================================
public sealed class PlanListPresenter : PlatformPresenter<PlanListState>
{
    private readonly ListPlatformPlansUseCase _list;

    public PlanListPresenter(ListPlatformPlansUseCase list, PlatformUseCaseScope scope, ILogger<PlanListPresenter> logger)
        : base(scope, PlanListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(PlatformPlanStatus? status, string? search, int page)
    {
        ListPlatformPlansResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(
                context,
                new ListPlatformPlansRequest { Status = status, Search = search, Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, Page = page },
                cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Plans is { } plans
            ? new PlanListState(plans.Items, plans.Page, plans.TotalPages, plans.TotalCount, response.Abilities, true, null)
            : State with { Loaded = true, Error = error });
    }
}
