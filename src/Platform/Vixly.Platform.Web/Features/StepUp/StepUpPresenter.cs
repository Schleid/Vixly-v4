using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.UseCases.MyAccount.BeginPasskeyStepUp;
using Vixly.Platform.Application.UseCases.MyAccount.ConfirmSecondFactor;
using Vixly.Platform.Application.UseCases.MyAccount.ConfirmSecondFactorWithPasskey;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.StepUp;

// =========================================================
// FILE: StepUpPresenter.cs
//
// PURPOSE:
// Behaviour of the step-up dialog (ADR-0006 section 5).
//
// RESPONSIBILITIES:
// - Send the code, or a recovery code, to ConfirmSecondFactor
// - Start and finish a passkey step-up; the component runs the
//   browser part in between
// - Report a wrong code, or that the session ended
//
// IMPORTANT NOTES:
// - The use case counts failures and ends the session after five
// - The component sends the user to sign in when SessionEnded
// =========================================================
public sealed class StepUpPresenter : PlatformPresenter<StepUpState>
{
    private readonly ConfirmSecondFactorUseCase _confirm;

    private readonly BeginPasskeyStepUpUseCase _beginPasskey;

    private readonly ConfirmSecondFactorWithPasskeyUseCase _confirmPasskey;

    public StepUpPresenter(
        ConfirmSecondFactorUseCase confirm,
        BeginPasskeyStepUpUseCase beginPasskey,
        ConfirmSecondFactorWithPasskeyUseCase confirmPasskey,
        PlatformUseCaseScope scope,
        ILogger<StepUpPresenter> logger)
        : base(scope, StepUpState.Initial, logger)
    {
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        _beginPasskey = beginPasskey ?? throw new ArgumentNullException(nameof(beginPasskey));
        _confirmPasskey = confirmPasskey ?? throw new ArgumentNullException(nameof(confirmPasskey));
    }

    public async Task<PlatformPasskeyStart?> BeginPasskeyAsync()
    {
        PlatformPasskeyStart? started = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            BeginPasskeyStepUpResponse response = await _beginPasskey.ExecuteAsync(context, new BeginPasskeyStepUpRequest(), cancellationToken);
            started = response.Succeeded ? new PlatformPasskeyStart(response.CeremonyToken, response.OptionsJson) : null;

            return response.Error;
        });

        SetState(State with { Error = error, SessionEnded = PlatformAuthErrors.SessionEnded.Equals(error) });

        return error is null ? started : null;
    }

    public void ReportPasskeyCancelled()
    {
        SetState(State with { Error = PlatformAuthErrors.PasskeyNotAccepted });
    }

    public async Task<bool> ConfirmPasskeyAsync(PlatformPasskeyStart started, string assertionJson)
    {
        ArgumentNullException.ThrowIfNull(started);

        bool confirmed = false;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            ConfirmSecondFactorWithPasskeyResponse response = await _confirmPasskey.ExecuteAsync(
                context,
                new ConfirmSecondFactorWithPasskeyRequest { CeremonyToken = started.CeremonyToken, AssertionJson = assertionJson ?? string.Empty },
                cancellationToken);
            confirmed = response.Succeeded;

            return response.Error;
        });

        SetState(State with { Error = error, SessionEnded = PlatformAuthErrors.SessionEnded.Equals(error) });

        return confirmed;
    }

    public void ToggleRecoveryCode()
    {
        SetState(State with { UseRecoveryCode = !State.UseRecoveryCode, Error = null });
    }

    public async Task<bool> ConfirmAsync(string code)
    {
        bool confirmed = false;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            ConfirmSecondFactorResponse response = await _confirm.ExecuteAsync(
                context,
                new ConfirmSecondFactorRequest { Code = code ?? string.Empty, UseRecoveryCode = State.UseRecoveryCode },
                cancellationToken);
            confirmed = response.Succeeded;

            return response.Error;
        });

        SetState(State with { Error = error, SessionEnded = PlatformAuthErrors.SessionEnded.Equals(error) });

        return confirmed;
    }
}
