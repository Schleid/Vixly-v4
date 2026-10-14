using System.Globalization;
using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Plans.GetPlatformPlan;
using Vixly.Platform.Application.UseCases.Plans.ReactivatePlatformPlan;
using Vixly.Platform.Application.UseCases.Plans.RetirePlatformPlan;
using Vixly.Platform.Application.UseCases.Plans.SetPlatformPlanCatalogue;
using Vixly.Platform.Application.UseCases.Plans.SetPlatformPlanTranslation;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Plans;

// =========================================================
// FILE: PlanPresenter.cs
//
// PURPOSE:
// Behaviour of the plan page summary.
//
// RESPONSIBILITIES:
// - Load the plan
// - Save a translation and the catalogue settings
// - Retire and reactivate the plan
//
// IMPORTANT NOTES:
// - Every change reloads the plan so all sections agree
// =========================================================
public sealed class PlanPresenter : PlatformPresenter<PlanState>
{
    private readonly GetPlatformPlanUseCase _get;

    private readonly SetPlatformPlanTranslationUseCase _translation;

    private readonly SetPlatformPlanCatalogueUseCase _catalogue;

    private readonly RetirePlatformPlanUseCase _retire;

    private readonly ReactivatePlatformPlanUseCase _reactivate;

    public PlanPresenter(
        GetPlatformPlanUseCase get,
        SetPlatformPlanTranslationUseCase translation,
        SetPlatformPlanCatalogueUseCase catalogue,
        RetirePlatformPlanUseCase retire,
        ReactivatePlatformPlanUseCase reactivate,
        PlatformUseCaseScope scope,
        ILogger<PlanPresenter> logger)
        : base(scope, PlanState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _translation = translation ?? throw new ArgumentNullException(nameof(translation));
        _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
        _retire = retire ?? throw new ArgumentNullException(nameof(retire));
        _reactivate = reactivate ?? throw new ArgumentNullException(nameof(reactivate));
    }

    public async Task LoadAsync(Guid platformPlanId)
    {
        GetPlatformPlanResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(
                context,
                new GetPlatformPlanRequest { PlatformPlanId = platformPlanId, Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName },
                cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Plan is { } plan
            ? State with { Plan = plan, Abilities = response.Abilities, Loaded = true, Version = State.Version + 1 }
            : State with { Loaded = true, Error = error });
    }

    public Task SaveTranslationAsync(Guid platformPlanId, string language, string? name, string? description)
    {
        return ChangeAsync(platformPlanId, "ui.plans.translation_saved", async (context, cancellationToken) => (await _translation.ExecuteAsync(
            context,
            new SetPlatformPlanTranslationRequest { PlatformPlanId = platformPlanId, Language = language, Name = name, Description = description },
            cancellationToken)).Error);
    }

    public Task SaveCatalogueAsync(Guid platformPlanId, bool isPublic, int displayOrder)
    {
        return ChangeAsync(platformPlanId, "ui.plans.catalogue_saved", async (context, cancellationToken) => (await _catalogue.ExecuteAsync(
            context,
            new SetPlatformPlanCatalogueRequest { PlatformPlanId = platformPlanId, IsPublic = isPublic, DisplayOrder = displayOrder },
            cancellationToken)).Error);
    }

    public Task RetireAsync(Guid platformPlanId)
    {
        return ChangeAsync(platformPlanId, "ui.plans.retired", async (context, cancellationToken) => (await _retire.ExecuteAsync(
            context, new RetirePlatformPlanRequest { PlatformPlanId = platformPlanId }, cancellationToken)).Error);
    }

    public Task ReactivateAsync(Guid platformPlanId)
    {
        return ChangeAsync(platformPlanId, "ui.plans.reactivated", async (context, cancellationToken) => (await _reactivate.ExecuteAsync(
            context, new ReactivatePlatformPlanRequest { PlatformPlanId = platformPlanId }, cancellationToken)).Error);
    }

    private async Task ChangeAsync(
        Guid platformPlanId,
        string doneKey,
        Func<Vixly.Platform.Application.Security.PlatformAuthorizationContext, CancellationToken, Task<Error?>> work)
    {
        Error? error = await RunUseCaseAsync(work);
        SetState(State with { DoneKey = error is null ? doneKey : null, Error = error });

        if (error is null)
        {
            await LoadAsync(platformPlanId);
        }
    }
}
