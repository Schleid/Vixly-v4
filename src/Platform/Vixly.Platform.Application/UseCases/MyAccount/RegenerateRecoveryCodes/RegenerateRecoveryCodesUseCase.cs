using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.MyAccount.RegenerateRecoveryCodes;

// =========================================================
// FILE: RegenerateRecoveryCodesUseCase.cs
//
// PURPOSE:
// The signed-in user replaces their recovery codes with 10 new
// ones.
//
// RESPONSIBILITIES:
// - Require a recent second factor
// - Replace every code (old ones stop working), audit, commit,
//   send a security notice, and return the new codes once
//
// IMPORTANT NOTES:
// - Only hashes are stored; the clear codes exist in this
//   response only
// =========================================================
public sealed class RegenerateRecoveryCodesUseCase
{
    private const string NoticeCode = "platform.notice.recovery_codes_replaced";

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformIdentityMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RegenerateRecoveryCodesUseCase(
        IPlatformStepUpGuard stepUp,
        IPlatformUserRepository users,
        IPlatformSecretGenerator secrets,
        IPlatformIdentityMailer mailer,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RegenerateRecoveryCodesResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RegenerateRecoveryCodesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (!await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken))
        {
            return RegenerateRecoveryCodesResponse.Failure(PlatformCommonErrors.StepUpRequired);
        }

        PlatformUser? user = await _users.GetAsync(context.PlatformUserId, cancellationToken);

        if (user is null)
        {
            return RegenerateRecoveryCodesResponse.Failure(PlatformCommonErrors.NotFound);
        }

        List<PlatformSecret> codes = Enumerable
            .Range(0, PlatformIdentityLimits.RecoveryCodeCount)
            .Select(_ => _secrets.NewRecoveryCode())
            .ToList();

        user.ReplaceRecoveryCodes(codes.Select(code => code.Hash).ToArray(), _timeProvider.GetUtcNow());
        _audit.Record(user.Id, "platform.account.recovery_codes_replaced", null, nameof(PlatformUser), user.Id);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RegenerateRecoveryCodesResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendSecurityNoticeAsync(user, NoticeCode, cancellationToken);

        return RegenerateRecoveryCodesResponse.Success(codes.Select(code => code.Value).ToList());
    }
}
