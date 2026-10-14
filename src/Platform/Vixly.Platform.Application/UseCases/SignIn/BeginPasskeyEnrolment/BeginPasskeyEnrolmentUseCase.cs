using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.SignIn.BeginPasskeyEnrolment;

// =========================================================
// FILE: BeginPasskeyEnrolmentUseCase.cs
//
// PURPOSE:
// Starts creating a passkey as the first second factor, during
// the first sign-in.
//
// RESPONSIBILITIES:
// - Accept only a sign-in challenge at the enrolment stage
// - Create registration options for this user and keep them in a
//   single-use, 5-minute ceremony bound to the user
//
// IMPORTANT NOTES:
// - The alternative to the authenticator app on the enrolment page
// =========================================================
public sealed class BeginPasskeyEnrolmentUseCase
{
    private readonly PlatformSignInSteps _steps;

    private readonly IPlatformPasskeys _passkeys;

    private readonly PlatformPasskeyCeremonies _ceremonies;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public BeginPasskeyEnrolmentUseCase(
        PlatformSignInSteps steps,
        IPlatformPasskeys passkeys,
        PlatformPasskeyCeremonies ceremonies,
        IPlatformUnitOfWork unitOfWork)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _passkeys = passkeys ?? throw new ArgumentNullException(nameof(passkeys));
        _ceremonies = ceremonies ?? throw new ArgumentNullException(nameof(ceremonies));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<BeginPasskeyEnrolmentResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        BeginPasskeyEnrolmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (PlatformSignInChallenge Challenge, PlatformUser User)? pending =
            await _steps.LoadAsync(request.ChallengeToken, PlatformSignInStage.Enrolment, cancellationToken);

        if (pending is not { } found)
        {
            return BeginPasskeyEnrolmentResponse.Failure(PlatformAuthErrors.SignInExpired);
        }

        string options = _passkeys.CreateRegistrationOptions(found.User, found.User.Passkeys.Select(passkey => passkey.CredentialId).ToList());
        string token = _ceremonies.Start(PlatformPasskeyCeremonyPurpose.Enrolment, found.User.Id, options);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? BeginPasskeyEnrolmentResponse.Success(token, options)
            : BeginPasskeyEnrolmentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
