using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users;

// =========================================================
// FILE: PlatformUserLifecycleWorkflow.cs
//
// PURPOSE:
// The shared steps of every administrator action on a staff
// account: lock, suspend, deactivate, reactivate.
//
// RESPONSIBILITIES:
// - Run the administration checks: permission (S-1), recent
//   second factor (ADR-0006 step-up), target authority
// - Apply the domain action, end the target's sessions when
//   asked, and write the audit entry
// - Commit everything in one SaveChanges (D-2)
//
// IMPORTANT NOTES:
// - Returns null on success, or the Error to report
// - Domain rules (own account, last SuperUser, reason) live
//   in PlatformUser; this class only orchestrates
// =========================================================
public sealed class PlatformUserLifecycleWorkflow
{
    private readonly PlatformUserAdministration _administration;

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformSessionRepository _sessions;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlatformUserLifecycleWorkflow(
        PlatformUserAdministration administration,
        IPlatformUserRepository users,
        IPlatformSessionRepository sessions,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Error?> RunAsync(
        PlatformAuthorizationContext context,
        PlatformUserLifecycleAction action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(action);

        PlatformUserTarget target = await _administration.LoadTargetAsync(
            context,
            action.PermissionCode,
            action.TargetUserId,
            requireStepUp: true,
            cancellationToken);

        if (target.User is not PlatformUser user)
        {
            return target.Error;
        }

        int otherSuperUsers = await _users.CountOtherActiveSuperUsersAsync(user.Id, cancellationToken);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        PlatformUserAccountStatus before = user.Account.Status;

        try
        {
            action.Apply(user, context.PlatformUserId, otherSuperUsers, utcNow);
        }
        catch (DomainException exception)
        {
            return exception.Error;
        }

        if (action.EndSessionsWith is PlatformSessionEndReason endReason)
        {
            foreach (PlatformUserSession session in await _sessions.ListOpenAsync(user.Id, cancellationToken))
            {
                session.End(endReason, utcNow);
            }
        }

        _audit.Record(
            context.PlatformUserId,
            action.AuditActionCode,
            action.PermissionCode,
            nameof(PlatformUser),
            user.Id,
            before: new { status = before.ToString() },
            after: new { status = user.Account.Status.ToString() },
            reason: user.Account.StatusReason);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? null
            : PlatformCommonErrors.ConcurrencyConflict;
    }
}
