using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans.DiscardPlatformPlanDraft;
using Vixly.Platform.Application.UseCases.Plans.PreviewPlatformPlanPrice;
using Vixly.Platform.Application.UseCases.Plans.SubmitPlatformPlanVersion;
using Vixly.Platform.Application.UseCases.Plans.StartPlatformPlanDraft;
using Vixly.Platform.Application.UseCases.Plans.UpdatePlatformPlanDraft;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Plans;

// =========================================================
// FILE: PlanDraftPresenter.cs
//
// PURPOSE:
// Behaviour of the plan versions section (D-S2).
//
// RESPONSIBILITIES:
// - Save the draft, submit it for CFO approval (D-S26), start or discard a draft
// - Preview the price of a period
//
// IMPORTANT NOTES:
// - Publishing never changes what existing subscribers pay this period (owner, D-S2)
// =========================================================
public sealed class PlanDraftPresenter : PlatformPresenter<PlanDraftState>
{
    private readonly UpdatePlatformPlanDraftUseCase _update;

    private readonly SubmitPlatformPlanVersionUseCase _submit;

    private readonly StartPlatformPlanDraftUseCase _start;

    private readonly DiscardPlatformPlanDraftUseCase _discard;

    private readonly PreviewPlatformPlanPriceUseCase _preview;

    public PlanDraftPresenter(
        UpdatePlatformPlanDraftUseCase update,
        SubmitPlatformPlanVersionUseCase submit,
        StartPlatformPlanDraftUseCase start,
        DiscardPlatformPlanDraftUseCase discard,
        PreviewPlatformPlanPriceUseCase preview,
        PlatformUseCaseScope scope,
        ILogger<PlanDraftPresenter> logger)
        : base(scope, PlanDraftState.Initial, logger)
    {
        _update = update ?? throw new ArgumentNullException(nameof(update));
        _submit = submit ?? throw new ArgumentNullException(nameof(submit));
        _start = start ?? throw new ArgumentNullException(nameof(start));
        _discard = discard ?? throw new ArgumentNullException(nameof(discard));
        _preview = preview ?? throw new ArgumentNullException(nameof(preview));
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public Task<bool> SaveAsync(Guid platformPlanId, PlanDraftModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return RunAsync("ui.plans.draft_saved", async (context, cancellationToken) => (await _update.ExecuteAsync(
            context,
            new UpdatePlatformPlanDraftRequest
            {
                PlatformPlanId = platformPlanId,
                CurrencyCode = model.CurrencyCode,
                MonthlyPricePerLicence = model.MonthlyPricePerLicence,
                AnnualPricePerLicence = model.AnnualPricePerLicence,
                SetupFee = model.SetupFee,
                MinimumLicences = model.MinimumLicences,
                TrialDays = model.TrialDays,
                MaxLicensedDevices = model.MaxLicensedDevices,
                MaxWarehouses = model.MaxWarehouses,
                Features = model.Features.ToArray()
            },
            cancellationToken)).Error);
    }

    public Task<bool> SubmitAsync(Guid platformPlanId)
    {
        return RunAsync("ui.plans.submitted", async (context, cancellationToken) => (await _submit.ExecuteAsync(
            context, new SubmitPlatformPlanVersionRequest { PlatformPlanId = platformPlanId }, cancellationToken)).Error);
    }

    public Task<bool> StartDraftAsync(Guid platformPlanId)
    {
        return RunAsync("ui.plans.draft_started", async (context, cancellationToken) => (await _start.ExecuteAsync(
            context, new StartPlatformPlanDraftRequest { PlatformPlanId = platformPlanId }, cancellationToken)).Error);
    }

    public Task<bool> DiscardDraftAsync(Guid platformPlanId)
    {
        return RunAsync("ui.plans.draft_discarded", async (context, cancellationToken) => (await _discard.ExecuteAsync(
            context, new DiscardPlatformPlanDraftRequest { PlatformPlanId = platformPlanId }, cancellationToken)).Error);
    }

    public async Task PreviewAsync(Guid platformPlanId, PlatformSubscriptionTerm term, int licences)
    {
        PreviewPlatformPlanPriceResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _preview.ExecuteAsync(
                context,
                new PreviewPlatformPlanPriceRequest { PlatformPlanId = platformPlanId, Term = term, Licences = licences },
                cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Preview = SubscriptionDisplay.Money(response.PeriodAmount, response.CurrencyCode), Error = null }
            : State with { Preview = null, Error = error });
    }

    private async Task<bool> RunAsync(string doneKey, Func<PlatformAuthorizationContext, CancellationToken, Task<Error?>> work)
    {
        Error? error = await RunUseCaseAsync(work);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return false;
        }

        SetState(new PlanDraftState(false, null, error is null ? doneKey : null, error));

        return error is null;
    }
}
