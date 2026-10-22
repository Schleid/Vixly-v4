using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInEnrolment;

// =========================================================
// FILE: CompleteOperationsSignInEnrolmentUseCase.cs
//
// PURPOSE:
// Confirms the authenticator app, issues ten recovery codes and starts the session (Slice 1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Recovery codes are shown once; only their hashes are stored
// =========================================================
public sealed class CompleteOperationsSignInEnrolmentUseCase
{
    private readonly OperationsSignInSteps _steps;

    private readonly IOperationsAuthenticatorCodes _codes;

    private readonly IOperationsSecretGenerator _secrets;

    public CompleteOperationsSignInEnrolmentUseCase(
        OperationsSignInSteps steps,
        IOperationsAuthenticatorCodes codes,
        IOperationsSecretGenerator secrets)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
    }

    public async Task<CompleteOperationsSignInEnrolmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CompleteOperationsSignInEnrolmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsSignInChallenge Challenge, OperationsUser User)? pending =
            await _steps.LoadAsync(request.ChallengeToken, OperationsSignInStage.Enrolment, cancellationToken);

        if (pending is not { } found || found.User.AuthenticatorApp is null)
        {
            return CompleteOperationsSignInEnrolmentResponse.Failure(OperationsAuthErrors.SignInExpired);
        }

        DateTimeOffset utcNow = _steps.UtcNow;
        long? timeStep = _codes.Verify(found.User.AuthenticatorApp.EncryptedSecret, request.Code, utcNow);

        if (timeStep is null)
        {
            await _steps.RecordFailedAttemptAsync(found.Challenge, found.User, "ops.auth.code_failed", cancellationToken);
            return CompleteOperationsSignInEnrolmentResponse.Failure(OperationsAuthErrors.CodeInvalid);
        }

        OperationsSecret[] recoveryCodes = Enumerable.Range(0, OperationsIdentityLimits.RecoveryCodeCount).Select(_ => _secrets.NewRecoveryCode()).ToArray();

        try
        {
            found.User.ConfirmAuthenticatorApp(timeStep.Value, utcNow);
            found.User.ReplaceRecoveryCodes(recoveryCodes.Select(code => code.Hash).ToArray(), utcNow);
        }
        catch (DomainException exception)
        {
            return CompleteOperationsSignInEnrolmentResponse.Failure(exception.Error);
        }

        OperationsIssuedSession? session = await _steps.CompleteAsync(context, found.Challenge, found.User, "enrolment", cancellationToken);

        return session is null
            ? CompleteOperationsSignInEnrolmentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict)
            : CompleteOperationsSignInEnrolmentResponse.Success(session.Token, recoveryCodes.Select(code => code.Value).ToArray());
    }
}
