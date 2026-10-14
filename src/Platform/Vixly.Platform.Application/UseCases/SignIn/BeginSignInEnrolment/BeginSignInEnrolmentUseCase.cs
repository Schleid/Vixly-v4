using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.SignIn.BeginSignInEnrolment;

// =========================================================
// FILE: BeginSignInEnrolmentUseCase.cs
//
// PURPOSE:
// Starts adding an authenticator app for a user who has no second factor yet, during sign-in.
//
// RESPONSIBILITIES:
// - Create and store an encrypted authenticator secret, or
//   reopen the one still waiting for its first code
// - Return the manual key and QR URI
//
// IMPORTANT NOTES:
// - Reopening matters: a new secret on every page load would
//   leave the phone with an old QR and every code refused
// - Passkey enrolment is the preferred path and is added with
//   the WebAuthn adapter; this is the fallback (ADR-0006)
// =========================================================
public sealed class BeginSignInEnrolmentUseCase
{
    private readonly PlatformSignInSteps _steps;

    private readonly IPlatformAuthenticatorCodes _codes;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public BeginSignInEnrolmentUseCase(
        PlatformSignInSteps steps,
        IPlatformAuthenticatorCodes codes,
        IPlatformUnitOfWork unitOfWork)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<BeginSignInEnrolmentResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        BeginSignInEnrolmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (PlatformSignInChallenge Challenge, PlatformUser User)? pending =
            await _steps.LoadAsync(request.ChallengeToken, PlatformSignInStage.Enrolment, cancellationToken);

        if (pending is not { } found)
        {
            return BeginSignInEnrolmentResponse.Failure(PlatformAuthErrors.SignInExpired);
        }

        PlatformUserAuthenticatorApp? pendingApp = found.User.AuthenticatorApp;
        PlatformAuthenticatorEnrolment? reopened = pendingApp is { IsConfirmed: false }
            ? _codes.Reopen(pendingApp.EncryptedSecret, found.User.Email.Value)
            : null;

        if (reopened is not null)
        {
            return BeginSignInEnrolmentResponse.Success(reopened.ManualKey, reopened.OtpAuthUri);
        }

        PlatformAuthenticatorEnrolment enrolment = _codes.CreateEnrolment(found.User.Email.Value);

        try
        {
            found.User.BeginAuthenticatorEnrolment(enrolment.EncryptedSecret, _steps.UtcNow);
        }
        catch (DomainException exception)
        {
            return BeginSignInEnrolmentResponse.Failure(exception.Error);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? BeginSignInEnrolmentResponse.Success(enrolment.ManualKey, enrolment.OtpAuthUri)
            : BeginSignInEnrolmentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
