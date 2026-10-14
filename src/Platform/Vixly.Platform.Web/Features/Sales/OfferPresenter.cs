using System.Globalization;
using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans.ListPlatformPlans;
using Vixly.Platform.Application.UseCases.Sales.GetPlatformOffer;
using Vixly.Platform.Application.UseCases.Sales.PreparePlatformOffer;
using Vixly.Platform.Application.UseCases.Sales.UpdatePlatformOffer;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: OfferPresenter.cs
//
// PURPOSE:
// Prepares, reads and edits an offer link (D-S21).
//
// RESPONSIBILITIES:
// - Run the use cases for its component and hold the state
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// =========================================================
public sealed class OfferPresenter : PlatformPresenter<OfferEditState>
{
    private readonly PreparePlatformOfferUseCase _prepare;

    private readonly GetPlatformOfferUseCase _get;

    private readonly UpdatePlatformOfferUseCase _update;

    private readonly ListPlatformPlansUseCase _plans;

    public OfferPresenter(
        PreparePlatformOfferUseCase prepare,
        GetPlatformOfferUseCase get,
        UpdatePlatformOfferUseCase update,
        ListPlatformPlansUseCase plans,
        PlatformUseCaseScope scope,
        ILogger<OfferPresenter> logger)
        : base(scope, OfferEditState.Initial, logger)
    {
        _prepare = prepare ?? throw new ArgumentNullException(nameof(prepare));
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _update = update ?? throw new ArgumentNullException(nameof(update));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
    }

    public async Task LoadAsync(Guid? offerId)
    {
        GetPlatformOfferResponse? response = null;
        IReadOnlyList<PlanChoice> plans = Array.Empty<PlanChoice>();
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            (plans, Error? failed) = await PlanChoices.LoadAsync(_plans, context, cancellationToken);

            if (failed is not null || offerId is not Guid id)
            {
                return failed;
            }

            response = await _get.ExecuteAsync(context, new GetPlatformOfferRequest { OfferId = id, Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName }, cancellationToken);
            return response.Error;
        });

        SetState(error is null
            ? State with { Offer = response?.Offer, Abilities = response?.Abilities ?? State.Abilities, Plans = plans, Loaded = true, Version = State.Version + 1 }
            : State with { Loaded = true, Error = error });
    }

    public async Task<Guid?> PrepareAsync(OfferFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        PreparePlatformOfferResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _prepare.ExecuteAsync(
                context,
                new PreparePlatformOfferRequest
                {
                    CompanyName = model.CompanyName ?? string.Empty,
                    BoundEmail = model.BoundEmail,
                    Reason = model.Reason ?? string.Empty,
                    PlatformPlanId = model.PlatformPlanId,
                    Term = model.Term,
                    Licences = model.Licences,
                    Discount = model.HasDiscount ? model.Discount.ToInput() : null,
                    SetupFeeOverride = model.SetupFeeOverride,
                    Charges = model.ChargeItems(),
                    StartTrial = model.StartTrial,
                    ExpiresUtc = SalesDisplay.EndOfDay(model.ExpiresOn) ?? DateTimeOffset.MinValue,
                    UsesAllowed = model.UsesAllowed
                },
                cancellationToken);
            return response.Error;
        });

        SetState(State with { Error = error });

        return error is null ? response?.OfferId : null;
    }

    public async Task<bool> SaveAsync(Guid offerId, OfferFormModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _update.ExecuteAsync(
            context,
            new UpdatePlatformOfferRequest
            {
                OfferId = offerId,
                CompanyName = model.CompanyName ?? string.Empty,
                BoundEmail = model.BoundEmail,
                Reason = model.Reason ?? string.Empty,
                PlatformPlanId = model.PlatformPlanId,
                Term = model.Term,
                Licences = model.Licences,
                Discount = model.HasDiscount ? model.Discount.ToInput() : null,
                SetupFeeOverride = model.SetupFeeOverride,
                Charges = model.ChargeItems(),
                StartTrial = model.StartTrial,
                ExpiresUtc = SalesDisplay.EndOfDay(model.ExpiresOn) ?? DateTimeOffset.MinValue,
                UsesAllowed = model.UsesAllowed
            },
            cancellationToken)).Error);

        SetState(State with { DoneKey = error is null ? "ui.offers.saved" : null, Error = error });

        if (error is null)
        {
            await LoadAsync(offerId);
        }

        return error is null;
    }
}
