using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.MyAccount.RemoveMyPasskey;

// =========================================================
// FILE: RemoveMyPasskeyUseCase.cs
//
// PURPOSE:
// The signed-in user removes one of their passkeys.
//
// RESPONSIBILITIES:
// - Require a recent second factor
// - Remove it unless it is the last second factor (domain), audit,
//   commit, then send a security notice
//
// IMPORTANT NOTES:
// - Removing a lost device's passkey is the user's first step; an
//   administrator can reset all second factors instead
// =========================================================
public sealed class RemoveMyPasskeyUseCase
{
    private const string NoticeCode = "platform.notice.passkey_removed";

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformIdentityMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RemoveMyPasskeyUseCase(
        IPlatformStepUpGuard stepUp,
        IPlatformUserRepository users,
        IPlatformIdentityMailer mailer,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RemoveMyPasskeyResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RemoveMyPasskeyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (!await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken))
        {
            return RemoveMyPasskeyResponse.Failure(PlatformCommonErrors.StepUpRequired);
        }

        PlatformUser? user = await _users.GetAsync(context.PlatformUserId, cancellationToken);
        PlatformUserPasskey? passkey = user?.Passkeys.FirstOrDefault(candidate => candidate.Id == request.PasskeyId);

        if (user is null || passkey is null)
        {
            return RemoveMyPasskeyResponse.Failure(PlatformIdentityErrors.PasskeyNotFound);
        }

        try
        {
            user.RemovePasskey(passkey.Id, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return RemoveMyPasskeyResponse.Failure(exception.Error);
        }

        _audit.Record(user.Id, "platform.account.passkey_removed", null, nameof(PlatformUser), user.Id, before: new { passkey = passkey.Name });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RemoveMyPasskeyResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendSecurityNoticeAsync(user, NoticeCode, cancellationToken);

        return RemoveMyPasskeyResponse.Success();
    }
}
