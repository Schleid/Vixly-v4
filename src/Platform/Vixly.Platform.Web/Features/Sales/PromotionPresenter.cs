using System.Globalization;
using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans.ListPlatformPlans;
using Vixly.Platform.Application.UseCases.Sales.GetPlatformPromotion;
using Vixly.Platform.Application.UseCases.Sales.SetPlatformPromotionName;
using Vixly.Platform.Application.UseCases.Sales.SubmitPlatformPromotion;
using Vixly.Platform.Application.UseCases.Sales.UpdatePlatformPromotion;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: PromotionPresenter.cs
//
// PURPOSE:
// Reads, edits, names and submits a promotion (D-S20, D-S26).
//
// RESPONSIBILITIES:
// - Run the use cases for its component and hold the state
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// - Five use cases, the scope and the logger: within the seven-dependency budget (UI-7)
// =========================================================
public sealed class PromotionPresenter : PlatformPresenter<PromotionEditState>
{
    private readonly GetPlatformPromotionUseCase _get;

    private readonly UpdatePlatformPromotionUseCase _update;

    private readonly SetPlatformPromotionNameUseCase _name;

    private readonly SubmitPlatformPromotionUseCase _submit;

    private readonly ListPlatformPlansUseCase _plans;

    public PromotionPresenter(
        GetPlatformPromotionUseCase get,
        UpdatePlatformPromotionUseCase update,
        SetPlatformPromotionNameUseCase name,
        SubmitPlatformPromotionUseCase submit,
        ListPlatformPlansUseCase plans,
        PlatformUseCaseScope scope,
        ILogger<PromotionPresenter> logger)
        : base(scope, PromotionEditState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _update = update ?? throw new ArgumentNullException(nameof(update));
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _submit = submit ?? throw new ArgumentNullException(nameof(submit));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
    }

    public async Task LoadAsync(Guid? promotionId)
    {
        GetPlatformPromotionResponse? response = null;
        IReadOnlyList<PlanChoice> plans = Array.Empty<PlanChoice>();
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            (plans, Error? failed) = await PlanChoices.LoadAsync(_plans, context, cancellationToken);

            if (failed is not null || promotionId is not Guid id)
            {
                return failed;
            }

            response = await _get.ExecuteAsync(context, new GetPlatformPromotionRequest { PromotionId = id, Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName }, cancellationToken);
            return response.Error;
        });

        SetState(error is null
            ? State with { Promotion = response?.Promotion, Abilities = response?.Abilities ?? State.Abilities, Plans = plans, Loaded = true, Version = State.Version + 1 }
            : State with { Loaded = true, Error = error });
    }

    public Task SaveAsync(Guid promotionId, PromotionFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return ChangeAsync(promotionId, "ui.promotions.saved", async (context, cancellationToken) => (await _update.ExecuteAsync(
            context,
            new UpdatePlatformPromotionRequest
            {
                PromotionId = promotionId,
                Terms = model.Terms.ToInput(),
                ValidFromUtc = SalesDisplay.StartOfDay(model.ValidFrom) ?? DateTimeOffset.UtcNow,
                ValidToUtc = SalesDisplay.StartOfDay(model.ValidTo?.AddDays(1)),
                PlanIds = model.PlanIds.ToArray(),
                Term = model.Term,
                AtRegistration = model.AtRegistration,
                ByStaff = model.ByStaff,
                RedemptionLimit = model.RedemptionLimit
            },
            cancellationToken)).Error);
    }

    public Task SetNameAsync(Guid promotionId, string language, string? name)
    {
        return ChangeAsync(promotionId, "ui.promotions.name_saved", async (context, cancellationToken) => (await _name.ExecuteAsync(
            context, new SetPlatformPromotionNameRequest { PromotionId = promotionId, Language = language, Name = name ?? string.Empty }, cancellationToken)).Error);
    }

    public Task SubmitAsync(Guid promotionId)
    {
        return ChangeAsync(promotionId, "ui.promotions.submitted", async (context, cancellationToken) => (await _submit.ExecuteAsync(
            context, new SubmitPlatformPromotionRequest { PromotionId = promotionId }, cancellationToken)).Error);
    }

    private async Task ChangeAsync(Guid promotionId, string doneKey, Func<PlatformAuthorizationContext, CancellationToken, Task<Error?>> work)
    {
        Error? error = await RunUseCaseAsync(work);
        SetState(State with { DoneKey = error is null ? doneKey : null, Error = error });

        if (error is null)
        {
            await LoadAsync(promotionId);
        }
    }
}
