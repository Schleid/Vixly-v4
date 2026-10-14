using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.SignIn.CompletePasskeySignIn;

// =========================================================
// FILE: CompletePasskeySignInUseCase.cs
//
// PURPOSE:
// Finishes "Sign in with a passkey": one step stands for both
// factors (ADR-0006 section 2).
//
// RESPONSIBILITIES:
// - Spend the ceremony, find the passkey owner, verify the answer
//   with the stored key and counter
// - Refuse accounts that cannot sign in (status, lockout)
// - Record the use, open a session, audit, commit
//
// IMPORTANT NOTES:
// - A passkey sign-in never touches the password or its lockout
//   counter
// - Failures are audited with the user when the passkey is
//   known, so a stolen device shows up in the log
// =========================================================
public sealed class CompletePasskeySignInUseCase
{
    private readonly PlatformPasskeyCeremonies _ceremonies;

    private readonly IPlatformPasskeys _passkeys;

    private readonly IPlatformUserRepository _users;

    private readonly PlatformSignInSteps _steps;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public CompletePasskeySignInUseCase(
        PlatformPasskeyCeremonies ceremonies,
        IPlatformPasskeys passkeys,
        IPlatformUserRepository users,
        PlatformSignInSteps steps,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork)
    {
        _ceremonies = ceremonies ?? throw new ArgumentNullException(nameof(ceremonies));
        _passkeys = passkeys ?? throw new ArgumentNullException(nameof(passkeys));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CompletePasskeySignInResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        CompletePasskeySignInRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPasskeyCeremony? ceremony = await _ceremonies.UseAsync(
            request.CeremonyToken,
            PlatformPasskeyCeremonyPurpose.SignIn,
            null,
            cancellationToken);

        if (ceremony is null)
        {
            return CompletePasskeySignInResponse.Failure(PlatformAuthErrors.SignInExpired);
        }

        byte[]? credentialId = _passkeys.ReadCredentialId(request.AssertionJson);
        PlatformUser? user = credentialId is null ? null : await _users.FindByPasskeyAsync(credentialId, cancellationToken);
        PlatformUserPasskey? passkey = user?.Passkeys.FirstOrDefault(candidate => candidate.Matches(credentialId!));

        if (user is null || passkey is null || !user.Account.IsSignInAllowed(_steps.UtcNow))
        {
            return await RefuseAsync(user, cancellationToken);
        }

        long? counter = await _passkeys.VerifyAssertionAsync(ceremony.OptionsJson, request.AssertionJson, passkey, cancellationToken);

        try
        {
            if (counter is null)
            {
                return await RefuseAsync(user, cancellationToken);
            }

            user.RecordPasskeySignIn(passkey.CredentialId, counter.Value, _steps.UtcNow);
        }
        catch (DomainException)
        {
            // A counter that went backwards means a cloned authenticator: refuse and audit.
            return await RefuseAsync(user, cancellationToken);
        }

        PlatformIssuedSession? session = await _steps.StartSessionAsync(user, "passkey", cancellationToken);

        return session is null
            ? CompletePasskeySignInResponse.Failure(PlatformCommonErrors.ConcurrencyConflict)
            : CompletePasskeySignInResponse.Success(session.SessionId, session.Token, session.ExpiresUtc);
    }

    private async Task<CompletePasskeySignInResponse> RefuseAsync(PlatformUser? user, CancellationToken cancellationToken)
    {
        _audit.Record(null, "platform.auth.passkey_failed", null, nameof(PlatformUser), user?.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CompletePasskeySignInResponse.Failure(PlatformAuthErrors.SignInFailed);
    }
}
