using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.MyAccount.CompletePasskeyRegistration;

// =========================================================
// FILE: CompletePasskeyRegistrationUseCase.cs
//
// PURPOSE:
// Finishes adding a passkey to the signed-in user's account.
//
// RESPONSIBILITIES:
// - Spend the user's registration ceremony and verify the answer
// - Refuse a passkey already registered to anyone
// - Register it, audit, commit, then send a security notice
//
// IMPORTANT NOTES:
// - The notice tells the user at once if someone else added a
//   passkey to their account
// =========================================================
public sealed class CompletePasskeyRegistrationUseCase
{
    private const string NoticeCode = "platform.notice.passkey_added";

    private readonly PlatformPasskeyCeremonies _ceremonies;

    private readonly IPlatformPasskeys _passkeys;

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformIdentityMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CompletePasskeyRegistrationUseCase(
        PlatformPasskeyCeremonies ceremonies,
        IPlatformPasskeys passkeys,
        IPlatformUserRepository users,
        IPlatformIdentityMailer mailer,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _ceremonies = ceremonies ?? throw new ArgumentNullException(nameof(ceremonies));
        _passkeys = passkeys ?? throw new ArgumentNullException(nameof(passkeys));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CompletePasskeyRegistrationResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CompletePasskeyRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPasskeyCeremony? ceremony = await _ceremonies.UseAsync(
            request.CeremonyToken,
            PlatformPasskeyCeremonyPurpose.Registration,
            context.PlatformUserId,
            cancellationToken);
        PlatformUser? user = await _users.GetAsync(context.PlatformUserId, cancellationToken);

        if (ceremony is null || user is null)
        {
            return CompletePasskeyRegistrationResponse.Failure(PlatformIdentityErrors.PasskeyCeremonyInvalid);
        }

        PlatformPasskeyCredential? credential = await _passkeys.VerifyRegistrationAsync(ceremony.OptionsJson, request.AttestationJson, cancellationToken);
        PlatformUserPasskey passkey;

        try
        {
            if (credential is null)
            {
                throw new DomainException(PlatformIdentityErrors.PasskeyInvalid);
            }

            if (await _users.FindByPasskeyAsync(credential.CredentialId, cancellationToken) is not null)
            {
                throw new DomainException(PlatformIdentityErrors.PasskeyAlreadyRegistered);
            }

            passkey = user.RegisterPasskey(
                credential.CredentialId,
                credential.PublicKey,
                credential.SignatureCounter,
                credential.Aaguid,
                request.Name,
                _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return CompletePasskeyRegistrationResponse.Failure(exception.Error);
        }

        _audit.Record(user.Id, "platform.account.passkey_added", null, nameof(PlatformUser), user.Id, after: new { passkey = passkey.Name });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CompletePasskeyRegistrationResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendSecurityNoticeAsync(user, NoticeCode, cancellationToken);

        return CompletePasskeyRegistrationResponse.Success(passkey.Id);
    }
}
