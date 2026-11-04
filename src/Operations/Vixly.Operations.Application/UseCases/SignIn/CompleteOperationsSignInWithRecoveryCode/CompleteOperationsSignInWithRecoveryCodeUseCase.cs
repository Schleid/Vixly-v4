using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithRecoveryCode;

// =========================================================
// FILE: CompleteOperationsSignInWithRecoveryCodeUseCase.cs
//
// PURPOSE:
// Finishes staff sign-in with a recovery code (Slice 1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Each recovery code works once
// =========================================================
public sealed class CompleteOperationsSignInWithRecoveryCodeUseCase
{
    private readonly OperationsSignInSteps _steps;

    private readonly IOperationsSecretGenerator _secrets;

    public CompleteOperationsSignInWithRecoveryCodeUseCase(
        OperationsSignInSteps steps,
        IOperationsSecretGenerator secrets)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
    }

    public async Task<CompleteOperationsSignInWithRecoveryCodeResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CompleteOperationsSignInWithRecoveryCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsSignInChallenge Challenge, OperationsUser User)? pending =
            await _steps.LoadAsync(request.ChallengeToken, OperationsSignInStage.SecondFactor, cancellationToken);

        if (pending is not { } found)
        {
            return CompleteOperationsSignInWithRecoveryCodeResponse.Failure(OperationsAuthErrors.SignInExpired);
        }

        try
        {
            found.User.UseRecoveryCode(_secrets.HashRecoveryCode(request.Code ?? string.Empty), _steps.UtcNow);
        }
        catch (DomainException)
        {
            await _steps.RecordFailedAttemptAsync(found.Challenge, found.User, "ops.auth.recovery_code_failed", cancellationToken);
            return CompleteOperationsSignInWithRecoveryCodeResponse.Failure(OperationsAuthErrors.CodeInvalid);
        }

        OperationsIssuedSession? session = await _steps.CompleteAsync(context, found.Challenge, found.User, "recovery_code", cancellationToken);

        return session is null
            ? CompleteOperationsSignInWithRecoveryCodeResponse.Failure(OperationsCommonErrors.ConcurrencyConflict)
            : CompleteOperationsSignInWithRecoveryCodeResponse.Success(session.Token, found.User.UnusedRecoveryCodeCount);
    }
}
