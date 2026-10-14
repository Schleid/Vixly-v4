using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.ConfirmSecondFactor;

// =========================================================
// FILE: ConfirmSecondFactorUseCase.cs
//
// PURPOSE:
// A signed-in staff user confirms it is them again before a
// sensitive action (ADR-0006 section 5, step-up).
//
// RESPONSIBILITIES:
// - Check the code against the user's authenticator app, or
//   spend a recovery code
// - On success, open the 5-minute step-up window on this
//   session and audit it
// - On failure, count it on the session and audit it; the
//   fifth failure in a row ends the session
//
// IMPORTANT NOTES:
// - No rate-limit policy can reach an interactive call, so the
//   session itself carries the attempt limit
// - Authenticator codes cannot be replayed: the last accepted
//   time step is kept (domain)
// - A passkey confirms through ConfirmSecondFactorWithPasskey
// =========================================================
public sealed class ConfirmSecondFactorUseCase
{
    private readonly PlatformStepUpSteps _steps;

    private readonly IPlatformAuthenticatorCodes _codes;

    private readonly IPlatformSecretGenerator _secrets;

    public ConfirmSecondFactorUseCase(
        PlatformStepUpSteps steps,
        IPlatformAuthenticatorCodes codes,
        IPlatformSecretGenerator secrets)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
    }

    public async Task<ConfirmSecondFactorResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ConfirmSecondFactorRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _steps.LoadAsync(context, cancellationToken) is not { } found)
        {
            return ConfirmSecondFactorResponse.Failure(PlatformAuthErrors.SessionEnded);
        }

        string method = request.UseRecoveryCode ? "recovery_code" : "authenticator_app";
        Error? error = TryAccept(found.User, request, _steps.UtcNow)
            ? await _steps.ConfirmAsync(found.Session, found.User, method, cancellationToken)
            : await _steps.FailAsync(found.Session, found.User, method, PlatformAuthErrors.CodeInvalid, cancellationToken);

        return error is null
            ? ConfirmSecondFactorResponse.Success()
            : ConfirmSecondFactorResponse.Failure(error);
    }

    private bool TryAccept(PlatformUser user, ConfirmSecondFactorRequest request, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return false;
        }

        try
        {
            if (request.UseRecoveryCode)
            {
                user.UseRecoveryCode(_secrets.HashRecoveryCode(request.Code), utcNow);
                return true;
            }

            long? timeStep = user.AuthenticatorApp?.IsConfirmed == true
                ? _codes.Verify(user.AuthenticatorApp.EncryptedSecret, request.Code, utcNow)
                : null;

            if (timeStep is null)
            {
                return false;
            }

            user.RecordAuthenticatorSignIn(timeStep.Value, utcNow);
            return true;
        }
        catch (DomainException)
        {
            // A reused or unknown code is a wrong code: the caller counts and audits it.
            return false;
        }
    }
}
