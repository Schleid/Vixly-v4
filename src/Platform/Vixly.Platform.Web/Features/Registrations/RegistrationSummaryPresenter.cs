using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Registrations.CancelPlatformTenantRegistration;
using Vixly.Platform.Application.UseCases.Registrations.GetPlatformTenantRegistration;
using Vixly.Platform.Application.UseCases.Registrations.ResendRegistrationVerification;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Registrations;

// =========================================================
// FILE: RegistrationSummaryPresenter.cs
//
// PURPOSE:
// Behaviour of the registration page.
//
// RESPONSIBILITIES:
// - Load the registration
// - Resend its verification link; cancel it with a reason
//
// IMPORTANT NOTES:
// - Cancelling frees a pending slug; nothing is deleted (D-T11)
// =========================================================
public sealed class RegistrationSummaryPresenter : PlatformPresenter<RegistrationSummaryState>
{
    private readonly GetPlatformTenantRegistrationUseCase _get;

    private readonly ResendRegistrationVerificationUseCase _resend;

    private readonly CancelPlatformTenantRegistrationUseCase _cancel;

    public RegistrationSummaryPresenter(
        GetPlatformTenantRegistrationUseCase get,
        ResendRegistrationVerificationUseCase resend,
        CancelPlatformTenantRegistrationUseCase cancel,
        PlatformUseCaseScope scope,
        ILogger<RegistrationSummaryPresenter> logger)
        : base(scope, RegistrationSummaryState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _resend = resend ?? throw new ArgumentNullException(nameof(resend));
        _cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
    }

    public async Task LoadAsync(Guid registrationId)
    {
        GetPlatformTenantRegistrationResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformTenantRegistrationRequest { RegistrationId = registrationId }, cancellationToken);

            return response.Error;
        });

        SetState(error is null && response?.Registration is not null
            ? State with { Registration = response.Registration, CanManage = response.CanManage }
            : State with { Registration = null, Error = error });
    }

    public void StartCancel()
    {
        SetState(State with { ConfirmingCancel = true, DoneKey = null, Error = null });
    }

    public void StopCancel()
    {
        SetState(State with { ConfirmingCancel = false, Error = null });
    }

    public Task ResendAsync(Guid registrationId)
    {
        return RunAndReloadAsync(registrationId, "ui.registrations.resent", async (context, cancellationToken) =>
            (await _resend.ExecuteAsync(context, new ResendRegistrationVerificationRequest { RegistrationId = registrationId }, cancellationToken)).Error);
    }

    public Task CancelAsync(Guid registrationId, string? reason)
    {
        return RunAndReloadAsync(registrationId, "ui.registrations.cancelled", async (context, cancellationToken) =>
            (await _cancel.ExecuteAsync(
                context,
                new CancelPlatformTenantRegistrationRequest { RegistrationId = registrationId, Reason = reason ?? string.Empty },
                cancellationToken)).Error);
    }

    private async Task RunAndReloadAsync(
        Guid registrationId,
        string doneKey,
        Func<PlatformAuthorizationContext, CancellationToken, Task<Error?>> work)
    {
        Error? error = await RunUseCaseAsync(work);

        if (error is not null)
        {
            SetState(State with { Error = error });
            return;
        }

        SetState(State with { ConfirmingCancel = false, DoneKey = doneKey, Error = null });
        await LoadAsync(registrationId);
    }
}
