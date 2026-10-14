using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.SignIn.CompletePasskeyEnrolment;

// =========================================================
// FILE: CompletePasskeyEnrolmentUseCase.cs
//
// PURPOSE:
// Finishes creating the first passkey during the first sign-in,
// then opens the session.
//
// RESPONSIBILITIES:
// - Accept only this user's enrolment ceremony and challenge
// - Verify the new passkey, refuse one registered to anyone
// - Register it, drop an authenticator app set up but never
//   confirmed, issue 10 recovery codes, audit, open the session
//   and send a security notice
//
// IMPORTANT NOTES:
// - A refused passkey counts as a failed attempt on the challenge
// - The session starts with a recent second factor, like the
//   authenticator path
// =========================================================
public sealed class CompletePasskeyEnrolmentUseCase
{
    private readonly PlatformSignInSteps _steps;

    private readonly PlatformPasskeyCeremonies _ceremonies;

    private readonly IPlatformPasskeys _passkeys;

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformIdentityMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    public CompletePasskeyEnrolmentUseCase(
        PlatformSignInSteps steps,
        PlatformPasskeyCeremonies ceremonies,
        IPlatformPasskeys passkeys,
        IPlatformUserRepository users,
        IPlatformSecretGenerator secrets,
        IPlatformIdentityMailer mailer,
        PlatformAuditTrail audit)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _ceremonies = ceremonies ?? throw new ArgumentNullException(nameof(ceremonies));
        _passkeys = passkeys ?? throw new ArgumentNullException(nameof(passkeys));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task<CompletePasskeyEnrolmentResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        CompletePasskeyEnrolmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (PlatformSignInChallenge Challenge, PlatformUser User)? pending =
            await _steps.LoadAsync(request.ChallengeToken, PlatformSignInStage.Enrolment, cancellationToken);

        if (pending is not { } found)
        {
            return CompletePasskeyEnrolmentResponse.Failure(PlatformAuthErrors.SignInExpired);
        }

        PlatformUser user = found.User;
        PlatformPasskeyCeremony? ceremony = await _ceremonies.UseAsync(
            request.CeremonyToken,
            PlatformPasskeyCeremonyPurpose.Enrolment,
            user.Id,
            cancellationToken);
        PlatformPasskeyCredential? credential = ceremony is null
            ? null
            : await _passkeys.VerifyRegistrationAsync(ceremony.OptionsJson, request.AttestationJson, cancellationToken);

        if (credential is null || await _users.FindByPasskeyAsync(credential.CredentialId, cancellationToken) is not null)
        {
            await _steps.RecordFailedAttemptAsync(found.Challenge, user, "platform.auth.passkey_failed", cancellationToken);
            return CompletePasskeyEnrolmentResponse.Failure(PlatformAuthErrors.PasskeyNotAccepted);
        }

        DateTimeOffset utcNow = _steps.UtcNow;
        PlatformSecret[] recoveryCodes = Enumerable
            .Range(0, PlatformIdentityLimits.RecoveryCodeCount)
            .Select(_ => _secrets.NewRecoveryCode())
            .ToArray();
        PlatformUserPasskey passkey;

        try
        {
            passkey = user.RegisterPasskey(
                credential.CredentialId,
                credential.PublicKey,
                credential.SignatureCounter,
                credential.Aaguid,
                request.Name,
                utcNow);

            if (user.AuthenticatorApp is { IsConfirmed: false })
            {
                user.RemoveAuthenticatorApp(utcNow);
            }

            user.ReplaceRecoveryCodes(recoveryCodes.Select(code => code.Hash).ToArray(), utcNow);
        }
        catch (DomainException exception)
        {
            return CompletePasskeyEnrolmentResponse.Failure(exception.Error);
        }

        _audit.Record(user.Id, "platform.account.passkey_added", null, nameof(PlatformUser), user.Id, after: new { passkey = passkey.Name });

        PlatformIssuedSession? session = await _steps.CompleteAsync(found.Challenge, user, "enrolment_passkey", cancellationToken);

        if (session is null)
        {
            return CompletePasskeyEnrolmentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendSecurityNoticeAsync(user, "platform.notice.passkey_added", cancellationToken);

        return CompletePasskeyEnrolmentResponse.Success(
            session.SessionId,
            session.Token,
            session.ExpiresUtc,
            recoveryCodes.Select(code => code.Value).ToArray());
    }
}
