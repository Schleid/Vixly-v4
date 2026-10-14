using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.ConfirmSecondFactorWithPasskey;

// =========================================================
// FILE: ConfirmSecondFactorWithPasskeyUseCase.cs
//
// PURPOSE:
// A signed-in staff user confirms it is them with a passkey
// (ADR-0006 section 5, step-up).
//
// RESPONSIBILITIES:
// - Spend the user's step-up ceremony and verify the answer with
//   one of their own passkeys
// - Open the 5-minute window, or count the failure (the fifth
//   ends the session)
//
// IMPORTANT NOTES:
// - A passkey of another account is a failure like a wrong code
// =========================================================
public sealed class ConfirmSecondFactorWithPasskeyUseCase
{
    private readonly PlatformStepUpSteps _steps;

    private readonly PlatformPasskeyCeremonies _ceremonies;

    private readonly IPlatformPasskeys _passkeys;

    public ConfirmSecondFactorWithPasskeyUseCase(
        PlatformStepUpSteps steps,
        PlatformPasskeyCeremonies ceremonies,
        IPlatformPasskeys passkeys)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _ceremonies = ceremonies ?? throw new ArgumentNullException(nameof(ceremonies));
        _passkeys = passkeys ?? throw new ArgumentNullException(nameof(passkeys));
    }

    public async Task<ConfirmSecondFactorWithPasskeyResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ConfirmSecondFactorWithPasskeyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _steps.LoadAsync(context, cancellationToken) is not { } found)
        {
            return ConfirmSecondFactorWithPasskeyResponse.Failure(PlatformAuthErrors.SessionEnded);
        }

        PlatformPasskeyCeremony? ceremony = await _ceremonies.UseAsync(
            request.CeremonyToken,
            PlatformPasskeyCeremonyPurpose.StepUp,
            found.User.Id,
            cancellationToken);
        Error? error = ceremony is not null && await AcceptAsync(found.User, ceremony, request.AssertionJson, cancellationToken)
            ? await _steps.ConfirmAsync(found.Session, found.User, "passkey", cancellationToken)
            : await _steps.FailAsync(found.Session, found.User, "passkey", PlatformAuthErrors.PasskeyNotAccepted, cancellationToken);

        return error is null
            ? ConfirmSecondFactorWithPasskeyResponse.Success()
            : ConfirmSecondFactorWithPasskeyResponse.Failure(error);
    }

    private async Task<bool> AcceptAsync(
        PlatformUser user,
        PlatformPasskeyCeremony ceremony,
        string assertionJson,
        CancellationToken cancellationToken)
    {
        byte[]? credentialId = _passkeys.ReadCredentialId(assertionJson);
        PlatformUserPasskey? passkey = credentialId is null
            ? null
            : user.Passkeys.FirstOrDefault(candidate => candidate.Matches(credentialId));

        if (passkey is null)
        {
            return false;
        }

        long? counter = await _passkeys.VerifyAssertionAsync(ceremony.OptionsJson, assertionJson, passkey, cancellationToken);

        try
        {
            if (counter is null)
            {
                return false;
            }

            user.RecordPasskeySignIn(passkey.CredentialId, counter.Value, _steps.UtcNow);
            return true;
        }
        catch (DomainException)
        {
            // A counter that went backwards means a cloned authenticator: count it as a failure.
            return false;
        }
    }
}
