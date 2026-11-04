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
// - The password confirms only for a person with no second
//   factor while Require MFA is No (owner, 2026-10-22)
// =========================================================
public sealed class ConfirmSecondFactorUseCase
{
    private readonly PlatformStepUpSteps _steps;

    private readonly IPlatformAuthenticatorCodes _codes;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformPasswordHasher _hasher;

    public ConfirmSecondFactorUseCase(
        PlatformStepUpSteps steps,
        IPlatformAuthenticatorCodes codes,
        IPlatformSecretGenerator secrets,
        IPlatformPasswordHasher hasher)
    {
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
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

        if (request.UsePassword)
        {
            bool allowed = await _steps.PasswordConfirmsAsync(found.User, cancellationToken);
            bool matches = allowed
                && found.User.Account.PasswordHash is { } hash
                && !string.IsNullOrEmpty(request.Code)
                && _hasher.Verify(request.Code, hash) != PlatformPasswordCheck.Failed;
            Error? outcome = matches
                ? await _steps.ConfirmAsync(found.Session, found.User, "password", cancellationToken)
                : await _steps.FailAsync(found.Session, found.User, "password", allowed ? PlatformAuthErrors.PasswordIncorrect : PlatformAuthErrors.CodeInvalid, cancellationToken);

            return outcome is null ? ConfirmSecondFactorResponse.Success() : ConfirmSecondFactorResponse.Failure(outcome);
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
