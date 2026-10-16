using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments.CheckPlatformMerchantAccount;
using Vixly.Platform.Application.UseCases.Payments.GetPlatformMerchantAccount;
using Vixly.Platform.Application.UseCases.Payments.UpdatePlatformMerchantAccount;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: PaymentsSettingsPresenter.cs
//
// PURPOSE:
// Behaviour of the Payments settings page (D-P8, D-P9, D-P11).
//
// RESPONSIBILITIES:
// - Load the merchant account and the provider configuration
// - Save the publishable key, currencies and on/off; check the connection
//
// IMPORTANT NOTES:
// - Keys are never entered here: they come from configuration (S-11)
// - Saving asks for step-up
// =========================================================
public sealed class PaymentsSettingsPresenter : PlatformPresenter<PaymentsSettingsState>
{
    private readonly GetPlatformMerchantAccountUseCase _get;

    private readonly UpdatePlatformMerchantAccountUseCase _update;

    private readonly CheckPlatformMerchantAccountUseCase _check;

    public PaymentsSettingsPresenter(GetPlatformMerchantAccountUseCase get, UpdatePlatformMerchantAccountUseCase update, CheckPlatformMerchantAccountUseCase check, PlatformUseCaseScope scope, ILogger<PaymentsSettingsPresenter> logger)
        : base(scope, PaymentsSettingsState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _update = update ?? throw new ArgumentNullException(nameof(update));
        _check = check ?? throw new ArgumentNullException(nameof(check));
    }

    public async Task LoadAsync()
    {
        GetPlatformMerchantAccountResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformMerchantAccountRequest(), cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Account = response.Account, CanEdit = response.CanEdit, Loaded = true, Version = State.Version + 1 }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public async Task SaveAsync(string? publishableKey, string? currencies, bool enabled)
    {
        string[] codes = (currencies ?? string.Empty).Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _update.ExecuteAsync(
            context, new UpdatePlatformMerchantAccountRequest { PublishableKey = publishableKey, Currencies = codes, Enabled = enabled }, cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return;
        }

        SetState(State with { StepUpPending = false, DoneKey = error is null ? "ui.payments_settings.saved" : null, Error = error });

        if (error is null)
        {
            await LoadAsync();
        }
    }

    public async Task CheckAsync()
    {
        CheckPlatformMerchantAccountResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _check.ExecuteAsync(context, new CheckPlatformMerchantAccountRequest(), cancellationToken);
            return response.Error;
        });

        SetState(State with
        {
            DoneKey = error is null && response!.Connected ? "ui.payments_settings.connected" : null,
            Error = error ?? (response!.Connected ? null : Error.Create(response.ProblemCode ?? "platform.payments.provider_unavailable"))
        });
        await LoadAsync();
    }
}
