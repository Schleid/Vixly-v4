using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Plans.ListPlatformPlans;
using Vixly.Platform.Application.UseCases.Sales.CreatePlatformPromotion;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: PromotionCreatePresenter.cs
//
// PURPOSE:
// Designs a new promotion as a draft (D-S20).
//
// RESPONSIBILITIES:
// - Run the use cases for its component and hold the state
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// =========================================================
public sealed class PromotionCreatePresenter : PlatformPresenter<PromotionEditState>
{
    private readonly CreatePlatformPromotionUseCase _create;

    private readonly ListPlatformPlansUseCase _plans;

    public PromotionCreatePresenter(CreatePlatformPromotionUseCase create, ListPlatformPlansUseCase plans, PlatformUseCaseScope scope, ILogger<PromotionCreatePresenter> logger)
        : base(scope, PromotionEditState.Initial, logger)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
    }

    public async Task LoadAsync()
    {
        IReadOnlyList<PlanChoice> plans = Array.Empty<PlanChoice>();
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            (plans, Error? failed) = await PlanChoices.LoadAsync(_plans, context, cancellationToken);
            return failed;
        });

        SetState(State with { Plans = plans, Loaded = true, Error = error });
    }
    public async Task<Guid?> CreateAsync(PromotionFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        CreatePlatformPromotionResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _create.ExecuteAsync(
                context,
                new CreatePlatformPromotionRequest
                {
                    Code = model.Code ?? string.Empty,
                    Name = model.Name ?? string.Empty,
                    Terms = model.Terms.ToInput(),
                    ValidFromUtc = SalesDisplay.StartOfDay(model.ValidFrom) ?? DateTimeOffset.UtcNow,
                    ValidToUtc = SalesDisplay.StartOfDay(model.ValidTo?.AddDays(1)),
                    PlanIds = model.PlanIds.ToArray(),
                    Term = model.Term,
                    AtRegistration = model.AtRegistration,
                    ByStaff = model.ByStaff,
                    RedemptionLimit = model.RedemptionLimit
                },
                cancellationToken);
            return response.Error;
        });

        SetState(State with { CreatedId = error is null ? response?.PromotionId : null, Error = error });

        return error is null ? response?.PromotionId : null;
    }
}
