using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.RevokeOperationsStaffBadge;

// =========================================================
// FILE: RevokeOperationsStaffBadgeUseCase.cs
//
// PURPOSE:
// Revokes a staff member's badge, for example when it is lost (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anyone with Edit staff may revoke, without step-up: stopping a lost badge must be quick
// - The person's badge and PIN sessions end as well
// =========================================================
public sealed class RevokeOperationsStaffBadgeUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsQuickSignInRepository _quick;

    private readonly IOperationsSessionRepository _sessions;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RevokeOperationsStaffBadgeUseCase(
        OperationsAccess access,
        IOperationsUserRepository users,
        IOperationsQuickSignInRepository quick,
        IOperationsSessionRepository sessions,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _quick = quick ?? throw new ArgumentNullException(nameof(quick));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RevokeOperationsStaffBadgeResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RevokeOperationsStaffBadgeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.StaffEdit, false, cancellationToken);

        if (refused is not null)
        {
            return RevokeOperationsStaffBadgeResponse.Failure(refused);
        }

        if (await _users.GetAsync(request.UserId, cancellationToken) is not { } target)
        {
            return RevokeOperationsStaffBadgeResponse.Failure(OperationsCommonErrors.NotFound);
        }

        if (await _quick.FindActiveBadgeAsync(target.Id, cancellationToken) is not { } badge)
        {
            return RevokeOperationsStaffBadgeResponse.Success(false);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        badge.Revoke(access!.User.Id, utcNow);

        // A lost badge may already be in someone's hands: the sessions it opened end too.
        foreach (OperationsUserSession session in await _sessions.ListOpenAsync(target.Id, cancellationToken))
        {
            if (session.IsQuick)
            {
                session.End(OperationsSessionEndReason.EndedByAdministrator, utcNow);
            }
        }

        _audit.Record(access.User.Id, "ops.staff.badge_revoked", nameof(OperationsUser), target.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RevokeOperationsStaffBadgeResponse.Success(true)
            : RevokeOperationsStaffBadgeResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
