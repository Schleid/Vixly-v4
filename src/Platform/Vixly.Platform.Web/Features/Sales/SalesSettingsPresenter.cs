using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Sales.GetPlatformSalesSettings;
using Vixly.Platform.Application.UseCases.Sales.UpdatePlatformSalesSettings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: SalesSettingsPresenter.cs
//
// PURPOSE:
// Reads and saves the Sales settings (D-S22).
//
// RESPONSIBILITIES:
// - Run the use cases for its component and hold the state
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// =========================================================
public sealed class SalesSettingsPresenter : PlatformPresenter<SalesSettingsState>
{
    private readonly GetPlatformSalesSettingsUseCase _get;

    private readonly UpdatePlatformSalesSettingsUseCase _update;

    public SalesSettingsPresenter(GetPlatformSalesSettingsUseCase get, UpdatePlatformSalesSettingsUseCase update, PlatformUseCaseScope scope, ILogger<SalesSettingsPresenter> logger)
        : base(scope, SalesSettingsState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _update = update ?? throw new ArgumentNullException(nameof(update));
    }

    public async Task LoadAsync()
    {
        GetPlatformSalesSettingsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformSalesSettingsRequest(), cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Settings = response.Settings, CanEdit = response.CanEdit, Loaded = true, Version = State.Version + 1 }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public async Task SaveAsync(SalesSettingsModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _update.ExecuteAsync(
            context,
            new UpdatePlatformSalesSettingsRequest
            {
                MaxPercentWithoutApproval = model.MaxPercentWithoutApproval,
                FixedLimits = model.FixedLimits
                    .Where(row => !string.IsNullOrWhiteSpace(row.CurrencyCode))
                    .GroupBy(row => row.CurrencyCode.Trim().ToUpperInvariant(), StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.Last().Amount, StringComparer.Ordinal),
                NoEndWithoutApproval = model.NoEndWithoutApproval,
                MaxOfferValidityDays = model.MaxOfferValidityDays,
                DiscountEndNoticeDays = model.DiscountEndNoticeDays,
                PromotionCodesAtRegistration = model.PromotionCodesAtRegistration
            },
            cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return;
        }

        SetState(State with { StepUpPending = false, DoneKey = error is null ? "ui.sales_settings.saved" : null, Error = error });

        if (error is null)
        {
            await LoadAsync();
        }
    }
}
