using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.SignIn.CompleteSignInEnrolment;

// =========================================================
// FILE: CompleteSignInEnrolmentUseCase.cs
//
// PURPOSE:
// Confirms the new authenticator app with a first code, issues recovery codes and opens the session.
//
// RESPONSIBILITIES:
// - Verify the first code from the new authenticator app
// - Generate 10 recovery codes (stored hashed)
// - Open the session; enrolment, codes and session commit
//   together
//
// IMPORTANT NOTES:
// - Until this succeeds the user has no second factor and no
//   session: enrolment before access (ADR-0006 section 3)
// =========================================================
public sealed class CompleteSignInEnrolmentUseCase
{
    private readonly PlatformSignInSteps _steps;

    private readonly IPlatformAuthenticatorCodes _codes;

    private readonly IPlatformSecretGenerator _secrets;

    public CompleteSignInEnrolmentUseCase(
        PlatformSignInSteps steps,
        IPlatformAuthenticatorCodes codes,
        IPlatformSecretGenerator secrets)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
    }

    public async Task<CompleteSignInEnrolmentResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        CompleteSignInEnrolmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (PlatformSignInChallenge Challenge, PlatformUser User)? pending =
            await _steps.LoadAsync(request.ChallengeToken, PlatformSignInStage.Enrolment, cancellationToken);

        if (pending is not { } found || found.User.AuthenticatorApp is null)
        {
            return CompleteSignInEnrolmentResponse.Failure(PlatformAuthErrors.SignInExpired);
        }

        DateTimeOffset utcNow = _steps.UtcNow;
        long? timeStep = _codes.Verify(found.User.AuthenticatorApp.EncryptedSecret, request.Code, utcNow);

        if (timeStep is null)
        {
            await _steps.RecordFailedAttemptAsync(found.Challenge, found.User, "platform.auth.code_failed", cancellationToken);
            return CompleteSignInEnrolmentResponse.Failure(PlatformAuthErrors.CodeInvalid);
        }

        PlatformSecret[] recoveryCodes = Enumerable
            .Range(0, PlatformIdentityLimits.RecoveryCodeCount)
            .Select(_ => _secrets.NewRecoveryCode())
            .ToArray();

        try
        {
            found.User.ConfirmAuthenticatorApp(timeStep.Value, utcNow);
            found.User.ReplaceRecoveryCodes(recoveryCodes.Select(code => code.Hash).ToArray(), utcNow);
        }
        catch (DomainException exception)
        {
            return CompleteSignInEnrolmentResponse.Failure(exception.Error);
        }

        PlatformIssuedSession? session = await _steps.CompleteAsync(found.Challenge, found.User, "enrolment", cancellationToken);

        return session is null
            ? CompleteSignInEnrolmentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict)
            : CompleteSignInEnrolmentResponse.Success(
                session.SessionId,
                session.Token,
                session.ExpiresUtc,
                recoveryCodes.Select(code => code.Value).ToArray());
    }
}
