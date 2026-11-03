using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithPassword;

// =========================================================
// FILE: CompleteOperationsSignInWithPasswordUseCase.cs
//
// PURPOSE:
// Finishes the daily check of a badge or PIN sign-in with the password, for a person without an authenticator (Slice 12A).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only a pending sign-in of the Password stage is accepted; five wrong passwords end it
// =========================================================
public sealed class CompleteOperationsSignInWithPasswordUseCase
{
    private readonly OperationsSignInSteps _steps;

    private readonly IOperationsPasswordHasher _hasher;

    public CompleteOperationsSignInWithPasswordUseCase(
        OperationsSignInSteps steps,
        IOperationsPasswordHasher hasher)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
    }

    public async Task<CompleteOperationsSignInWithPasswordResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CompleteOperationsSignInWithPasswordRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsSignInChallenge Challenge, OperationsUser User)? pending =
            await _steps.LoadAsync(request.ChallengeToken, OperationsSignInStage.Password, cancellationToken);

        if (pending is not { } found)
        {
            return CompleteOperationsSignInWithPasswordResponse.Failure(OperationsAuthErrors.SignInExpired);
        }

        if (found.User.Account.PasswordHash is not { } hash || _hasher.Verify(request.Password ?? string.Empty, hash) == OperationsPasswordCheck.Failed)
        {
            await _steps.RecordFailedAttemptAsync(found.Challenge, found.User, "ops.auth.password_failed", cancellationToken);
            return CompleteOperationsSignInWithPasswordResponse.Failure(OperationsAuthErrors.PasswordWrong);
        }

        OperationsIssuedSession? session = await _steps.CompleteAsync(context, found.Challenge, found.User, "password", cancellationToken);

        return session is null
            ? CompleteOperationsSignInWithPasswordResponse.Failure(OperationsCommonErrors.ConcurrencyConflict)
            : CompleteOperationsSignInWithPasswordResponse.Success(session.Token);
    }
}
