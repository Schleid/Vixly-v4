using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.SignIn.CompleteSignInWithRecoveryCode;

// =========================================================
// FILE: CompleteSignInWithRecoveryCodeUseCase.cs
//
// PURPOSE:
// The second step of a staff sign-in, with a single-use recovery code.
//
// RESPONSIBILITIES:
// - Load the pending sign-in; refuse it when expired, used
//   or out of attempts
// - Check the code; count a wrong one against the pending
//   sign-in (5 attempts)
// - Open the session
//
// IMPORTANT NOTES:
// - The console tells the user how many codes are left and
//   suggests new ones when few remain
// =========================================================
public sealed class CompleteSignInWithRecoveryCodeUseCase
{
    private readonly PlatformSignInSteps _steps;

    private readonly IPlatformSecretGenerator _secrets;

    public CompleteSignInWithRecoveryCodeUseCase(PlatformSignInSteps steps, IPlatformSecretGenerator secrets)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
    }

    public async Task<CompleteSignInWithRecoveryCodeResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        CompleteSignInWithRecoveryCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (PlatformSignInChallenge Challenge, PlatformUser User)? pending =
            await _steps.LoadAsync(request.ChallengeToken, PlatformSignInStage.SecondFactor, cancellationToken);

        if (pending is not { } found)
        {
            return CompleteSignInWithRecoveryCodeResponse.Failure(PlatformAuthErrors.SignInExpired);
        }

        DateTimeOffset utcNow = _steps.UtcNow;

        try
        {
            found.User.UseRecoveryCode(_secrets.HashRecoveryCode(request.Code ?? string.Empty), utcNow);
        }
        catch (DomainException)
        {
            await _steps.RecordFailedAttemptAsync(found.Challenge, found.User, "platform.auth.recovery_code_failed", cancellationToken);
            return CompleteSignInWithRecoveryCodeResponse.Failure(PlatformAuthErrors.CodeInvalid);
        }

        PlatformIssuedSession? session = await _steps.CompleteAsync(found.Challenge, found.User, "recovery_code", cancellationToken);

        return session is null
            ? CompleteSignInWithRecoveryCodeResponse.Failure(PlatformCommonErrors.ConcurrencyConflict)
            : CompleteSignInWithRecoveryCodeResponse.Success(session.SessionId, session.Token, session.ExpiresUtc, found.User.UnusedRecoveryCodeCount);
    }
}
