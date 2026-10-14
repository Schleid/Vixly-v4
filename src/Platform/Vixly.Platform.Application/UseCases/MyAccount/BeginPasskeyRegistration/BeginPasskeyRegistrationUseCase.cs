using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.MyAccount.BeginPasskeyRegistration;

// =========================================================
// FILE: BeginPasskeyRegistrationUseCase.cs
//
// PURPOSE:
// Starts adding a passkey to the signed-in user's account.
//
// RESPONSIBILITIES:
// - Require a recent second factor (adding a way in is sensitive)
// - Refuse when the 10-passkey limit is reached
// - Build options that exclude passkeys the user already has, and
//   store them in a 5-minute ceremony
//
// IMPORTANT NOTES:
// - The passkey is discoverable (stored on the device) and needs
//   user verification, so it can sign in on its own
// =========================================================
public sealed class BeginPasskeyRegistrationUseCase
{
    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformPasskeys _passkeys;

    private readonly PlatformPasskeyCeremonies _ceremonies;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public BeginPasskeyRegistrationUseCase(
        IPlatformStepUpGuard stepUp,
        IPlatformUserRepository users,
        IPlatformPasskeys passkeys,
        PlatformPasskeyCeremonies ceremonies,
        IPlatformUnitOfWork unitOfWork)
    {
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _passkeys = passkeys ?? throw new ArgumentNullException(nameof(passkeys));
        _ceremonies = ceremonies ?? throw new ArgumentNullException(nameof(ceremonies));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<BeginPasskeyRegistrationResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        BeginPasskeyRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (!await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken))
        {
            return BeginPasskeyRegistrationResponse.Failure(PlatformCommonErrors.StepUpRequired);
        }

        PlatformUser? user = await _users.GetAsync(context.PlatformUserId, cancellationToken);

        if (user is null)
        {
            return BeginPasskeyRegistrationResponse.Failure(PlatformCommonErrors.NotFound);
        }

        if (user.Passkeys.Count >= PlatformIdentityLimits.MaxPasskeysPerUser)
        {
            return BeginPasskeyRegistrationResponse.Failure(PlatformIdentityErrors.PasskeyLimitReached);
        }

        string options = _passkeys.CreateRegistrationOptions(user, user.Passkeys.Select(passkey => passkey.CredentialId).ToList());
        string token = _ceremonies.Start(PlatformPasskeyCeremonyPurpose.Registration, user.Id, options);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? BeginPasskeyRegistrationResponse.Success(token, options)
            : BeginPasskeyRegistrationResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
