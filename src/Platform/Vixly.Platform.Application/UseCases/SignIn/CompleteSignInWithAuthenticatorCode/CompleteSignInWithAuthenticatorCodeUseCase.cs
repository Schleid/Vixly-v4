using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.SignIn.CompleteSignInWithAuthenticatorCode;

// =========================================================
// FILE: CompleteSignInWithAuthenticatorCodeUseCase.cs
//
// PURPOSE:
// The second step of a staff sign-in, with an authenticator-app code.
//
// RESPONSIBILITIES:
// - Load the pending sign-in; refuse it when expired, used
//   or out of attempts
// - Check the code; count a wrong one against the pending
//   sign-in (5 attempts)
// - Open the session
//
// IMPORTANT NOTES:
// - A code already used (same time step) counts as wrong:
//   no replay
// =========================================================
public sealed class CompleteSignInWithAuthenticatorCodeUseCase
{
    private readonly PlatformSignInSteps _steps;

    private readonly IPlatformAuthenticatorCodes _codes;

    public CompleteSignInWithAuthenticatorCodeUseCase(PlatformSignInSteps steps, IPlatformAuthenticatorCodes codes)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
    }

    public async Task<CompleteSignInWithAuthenticatorCodeResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        CompleteSignInWithAuthenticatorCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (PlatformSignInChallenge Challenge, PlatformUser User)? pending =
            await _steps.LoadAsync(request.ChallengeToken, PlatformSignInStage.SecondFactor, cancellationToken);

        if (pending is not { } found)
        {
            return CompleteSignInWithAuthenticatorCodeResponse.Failure(PlatformAuthErrors.SignInExpired);
        }

        DateTimeOffset utcNow = _steps.UtcNow;

        long? timeStep = found.User.AuthenticatorApp?.IsConfirmed == true
            ? _codes.Verify(found.User.AuthenticatorApp.EncryptedSecret, request.Code, utcNow)
            : null;

        try
        {
            if (timeStep is null)
            {
                throw new DomainException(PlatformIdentityErrors.AuthenticatorNotConfirmed);
            }

            found.User.RecordAuthenticatorSignIn(timeStep.Value, utcNow);
        }
        catch (DomainException)
        {
            await _steps.RecordFailedAttemptAsync(found.Challenge, found.User, "platform.auth.code_failed", cancellationToken);
            return CompleteSignInWithAuthenticatorCodeResponse.Failure(PlatformAuthErrors.CodeInvalid);
        }

        PlatformIssuedSession? session = await _steps.CompleteAsync(found.Challenge, found.User, "authenticator_app", cancellationToken);

        return session is null
            ? CompleteSignInWithAuthenticatorCodeResponse.Failure(PlatformCommonErrors.ConcurrencyConflict)
            : CompleteSignInWithAuthenticatorCodeResponse.Success(session.SessionId, session.Token, session.ExpiresUtc);
    }
}
