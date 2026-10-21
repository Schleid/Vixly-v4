using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignInEnrolment;

// =========================================================
// FILE: BeginOperationsSignInEnrolmentUseCase.cs
//
// PURPOSE:
// Starts authenticator app enrolment during sign-in, for staff who need a second factor and have none (Slice 1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Showing the page again shows the same pending secret
// =========================================================
public sealed class BeginOperationsSignInEnrolmentUseCase
{
    private readonly OperationsSignInSteps _steps;

    private readonly IOperationsAuthenticatorCodes _codes;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public BeginOperationsSignInEnrolmentUseCase(
        OperationsSignInSteps steps,
        IOperationsAuthenticatorCodes codes,
        IOperationsBrandRepository brands,
        IOperationsUnitOfWork unitOfWork)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<BeginOperationsSignInEnrolmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        BeginOperationsSignInEnrolmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsSignInChallenge Challenge, OperationsUser User)? pending =
            await _steps.LoadAsync(request.ChallengeToken, OperationsSignInStage.Enrolment, cancellationToken);

        if (pending is not { } found)
        {
            return BeginOperationsSignInEnrolmentResponse.Failure(OperationsAuthErrors.SignInExpired);
        }

        // The app shows the tenant name as the issuer, never the product (ADR-0012 W-1).
        string issuer = (await _brands.FindAsync(cancellationToken))?.DisplayName ?? found.User.Email.Value.Split('@')[1];
        OperationsUserAuthenticatorApp? pendingApp = found.User.AuthenticatorApp;
        OperationsAuthenticatorEnrolment? reopened = pendingApp is { IsConfirmed: false }
            ? _codes.Reopen(pendingApp.EncryptedSecret, issuer, found.User.Email.Value)
            : null;

        if (reopened is not null)
        {
            return BeginOperationsSignInEnrolmentResponse.Success(reopened.ManualKey, reopened.OtpAuthUri);
        }

        OperationsAuthenticatorEnrolment enrolment = _codes.CreateEnrolment(issuer, found.User.Email.Value);

        try
        {
            found.User.BeginAuthenticatorEnrolment(enrolment.EncryptedSecret, _steps.UtcNow);
        }
        catch (DomainException exception)
        {
            return BeginOperationsSignInEnrolmentResponse.Failure(exception.Error);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? BeginOperationsSignInEnrolmentResponse.Success(enrolment.ManualKey, enrolment.OtpAuthUri)
            : BeginOperationsSignInEnrolmentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
