using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithAuthenticatorCode;

// =========================================================
// FILE: CompleteOperationsSignInWithAuthenticatorCodeUseCase.cs
//
// PURPOSE:
// Finishes staff sign-in with an authenticator app code (Slice 1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A code is accepted once (time step); 5 wrong codes end the pending sign-in
// =========================================================
public sealed class CompleteOperationsSignInWithAuthenticatorCodeUseCase
{
    private readonly OperationsSignInSteps _steps;

    private readonly IOperationsAuthenticatorCodes _codes;

    public CompleteOperationsSignInWithAuthenticatorCodeUseCase(
        OperationsSignInSteps steps,
        IOperationsAuthenticatorCodes codes)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
    }

    public async Task<CompleteOperationsSignInWithAuthenticatorCodeResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CompleteOperationsSignInWithAuthenticatorCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsSignInChallenge Challenge, OperationsUser User)? pending =
            await _steps.LoadAsync(request.ChallengeToken, OperationsSignInStage.SecondFactor, cancellationToken);

        if (pending is not { } found)
        {
            return CompleteOperationsSignInWithAuthenticatorCodeResponse.Failure(OperationsAuthErrors.SignInExpired);
        }

        DateTimeOffset utcNow = _steps.UtcNow;
        long? timeStep = found.User.AuthenticatorApp?.IsConfirmed == true
            ? _codes.Verify(found.User.AuthenticatorApp.EncryptedSecret, request.Code, utcNow)
            : null;

        try
        {
            if (timeStep is null)
            {
                throw new DomainException(OperationsIdentityErrors.AuthenticatorNotConfirmed);
            }

            found.User.RecordAuthenticatorSignIn(timeStep.Value, utcNow);
        }
        catch (DomainException)
        {
            await _steps.RecordFailedAttemptAsync(found.Challenge, found.User, "ops.auth.code_failed", cancellationToken);
            return CompleteOperationsSignInWithAuthenticatorCodeResponse.Failure(OperationsAuthErrors.CodeInvalid);
        }

        OperationsIssuedSession? session = await _steps.CompleteAsync(context, found.Challenge, found.User, "authenticator_app", cancellationToken);

        return session is null
            ? CompleteOperationsSignInWithAuthenticatorCodeResponse.Failure(OperationsCommonErrors.ConcurrencyConflict)
            : CompleteOperationsSignInWithAuthenticatorCodeResponse.Success(session.Token);
    }
}
