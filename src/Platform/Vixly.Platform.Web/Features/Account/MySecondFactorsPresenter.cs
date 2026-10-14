using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.MyAccount.BeginPasskeyRegistration;
using Vixly.Platform.Application.UseCases.MyAccount.CompletePasskeyRegistration;
using Vixly.Platform.Application.UseCases.MyAccount.GetMyAccount;
using Vixly.Platform.Application.UseCases.MyAccount.RegenerateRecoveryCodes;
using Vixly.Platform.Application.UseCases.MyAccount.RemoveMyPasskey;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Account;

// =========================================================
// FILE: MySecondFactorsPresenter.cs
//
// PURPOSE:
// Behaviour of the sign-in methods card: authenticator app,
// recovery codes and passkeys of the signed-in user.
//
// RESPONSIBILITIES:
// - Load the account
// - Add a passkey in two steps around the browser ceremony; remove
//   one; replace the recovery codes and show the new ones once
// - When the Application asks for a step-up, remember what was
//   asked and run it again after the confirmation
//
// IMPORTANT NOTES:
// - The browser part (navigator.credentials) is in the component;
//   this class has no Blazor or JavaScript types (UI-4)
// =========================================================
public sealed class MySecondFactorsPresenter : PlatformPresenter<MySecondFactorsState>
{
    private readonly GetMyAccountUseCase _account;

    private readonly BeginPasskeyRegistrationUseCase _beginAdd;

    private readonly CompletePasskeyRegistrationUseCase _completeAdd;

    private readonly RemoveMyPasskeyUseCase _remove;

    private readonly RegenerateRecoveryCodesUseCase _regenerate;

    public MySecondFactorsPresenter(
        GetMyAccountUseCase account,
        BeginPasskeyRegistrationUseCase beginAdd,
        CompletePasskeyRegistrationUseCase completeAdd,
        RemoveMyPasskeyUseCase remove,
        RegenerateRecoveryCodesUseCase regenerate,
        PlatformUseCaseScope scope,
        ILogger<MySecondFactorsPresenter> logger)
        : base(scope, MySecondFactorsState.Initial, logger)
    {
        _account = account ?? throw new ArgumentNullException(nameof(account));
        _beginAdd = beginAdd ?? throw new ArgumentNullException(nameof(beginAdd));
        _completeAdd = completeAdd ?? throw new ArgumentNullException(nameof(completeAdd));
        _remove = remove ?? throw new ArgumentNullException(nameof(remove));
        _regenerate = regenerate ?? throw new ArgumentNullException(nameof(regenerate));
    }

    public async Task RegenerateCodesAsync()
    {
        RegenerateRecoveryCodesResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _regenerate.ExecuteAsync(context, new RegenerateRecoveryCodesRequest(), cancellationToken);
            return response.Error;
        });

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { CodesWaitForStepUp = true, Error = null, InfoKey = null });
            return;
        }

        SetState(State with
        {
            CodesWaitForStepUp = false,
            Error = error,
            InfoKey = null,
            NewRecoveryCodes = error is null ? response?.Codes ?? Array.Empty<string>() : Array.Empty<string>()
        });

        if (error is null)
        {
            await LoadAsync();
        }
    }

    public void DismissCodes()
    {
        SetState(State with { NewRecoveryCodes = Array.Empty<string>() });
    }

    public async Task LoadAsync()
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            GetMyAccountResponse response = await _account.ExecuteAsync(context, new GetMyAccountRequest(), cancellationToken);

            if (response.Account is not null)
            {
                SetState(State with { Account = response.Account });
            }

            return response.Error;
        });

        if (error is not null)
        {
            SetState(State with { Error = error });
        }
    }

    public async Task<PlatformPasskeyStart?> BeginAddAsync()
    {
        PlatformPasskeyStart? started = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            BeginPasskeyRegistrationResponse response = await _beginAdd.ExecuteAsync(context, new BeginPasskeyRegistrationRequest(), cancellationToken);
            started = response.Succeeded ? new PlatformPasskeyStart(response.CeremonyToken, response.OptionsJson) : null;

            return response.Error;
        });

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { AddWaitsForStepUp = true, Error = null, InfoKey = null });
            return null;
        }

        SetState(State with { AddWaitsForStepUp = false, Error = error, InfoKey = null });

        return error is null ? started : null;
    }

    public void ReportPasskeyCancelled()
    {
        SetState(State with { Error = null, InfoKey = "ui.passkey.cancelled" });
    }

    public async Task CompleteAddAsync(PlatformPasskeyStart started, string attestationJson, string name)
    {
        ArgumentNullException.ThrowIfNull(started);

        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
            (await _completeAdd.ExecuteAsync(
                context,
                new CompletePasskeyRegistrationRequest
                {
                    CeremonyToken = started.CeremonyToken,
                    AttestationJson = attestationJson ?? string.Empty,
                    Name = name ?? string.Empty
                },
                cancellationToken)).Error);

        SetState(State with { Error = error, InfoKey = error is null ? "ui.account.passkey_added" : null });

        if (error is null)
        {
            await LoadAsync();
        }
    }

    public async Task RemoveAsync(Guid passkeyId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
            (await _remove.ExecuteAsync(context, new RemoveMyPasskeyRequest { PasskeyId = passkeyId }, cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { RemovalWaitsForStepUp = passkeyId, Error = null, InfoKey = null });
            return;
        }

        SetState(State with { RemovalWaitsForStepUp = null, Error = error, InfoKey = error is null ? "ui.account.passkey_removed" : null });

        if (error is null)
        {
            await LoadAsync();
        }
    }

    public void CancelStepUp()
    {
        SetState(State with { AddWaitsForStepUp = false, RemovalWaitsForStepUp = null, CodesWaitForStepUp = false });
    }
}
