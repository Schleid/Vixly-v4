using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.MyAccount.BeginPasskeyStepUp;

// =========================================================
// FILE: BeginPasskeyStepUpUseCase.cs
//
// PURPOSE:
// Starts confirming it is you with a passkey (step-up).
//
// RESPONSIBILITIES:
// - Build options limited to the signed-in user's passkeys
// - Store them in a 5-minute ceremony bound to the user
//
// IMPORTANT NOTES:
// - Answers no_passkeys when the user has none yet
// =========================================================
public sealed class BeginPasskeyStepUpUseCase
{
    private readonly PlatformStepUpSteps _steps;

    private readonly IPlatformPasskeys _passkeys;

    private readonly PlatformPasskeyCeremonies _ceremonies;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public BeginPasskeyStepUpUseCase(
        PlatformStepUpSteps steps,
        IPlatformPasskeys passkeys,
        PlatformPasskeyCeremonies ceremonies,
        IPlatformUnitOfWork unitOfWork)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _passkeys = passkeys ?? throw new ArgumentNullException(nameof(passkeys));
        _ceremonies = ceremonies ?? throw new ArgumentNullException(nameof(ceremonies));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<BeginPasskeyStepUpResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        BeginPasskeyStepUpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _steps.LoadAsync(context, cancellationToken) is not { } found)
        {
            return BeginPasskeyStepUpResponse.Failure(PlatformAuthErrors.SessionEnded);
        }

        if (found.User.Passkeys.Count == 0)
        {
            return BeginPasskeyStepUpResponse.Failure(PlatformAuthErrors.NoPasskeys);
        }

        string options = _passkeys.CreateAssertionOptions(found.User.Passkeys.Select(passkey => passkey.CredentialId).ToList());
        string token = _ceremonies.Start(PlatformPasskeyCeremonyPurpose.StepUp, found.User.Id, options);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? BeginPasskeyStepUpResponse.Success(token, options)
            : BeginPasskeyStepUpResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
