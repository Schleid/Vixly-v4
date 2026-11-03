using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.IssueOperationsStaffBadge;

// =========================================================
// FILE: IssueOperationsStaffBadgeUseCase.cs
//
// PURPOSE:
// Issues a new badge to a staff member, revoking the one in use (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs Edit staff with a fresh second factor (a badge is a way in)
// - The code is returned once, for printing; only its hash is kept
// - The old badge is revoked first, in its own commit, so the one-badge-per-person index holds
// =========================================================
public sealed class IssueOperationsStaffBadgeUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsQuickSignInRepository _quick;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public IssueOperationsStaffBadgeUseCase(
        OperationsAccess access,
        IOperationsUserRepository users,
        IOperationsQuickSignInRepository quick,
        IOperationsSecretGenerator secrets,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _quick = quick ?? throw new ArgumentNullException(nameof(quick));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<IssueOperationsStaffBadgeResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        IssueOperationsStaffBadgeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.StaffEdit, true, cancellationToken);

        if (refused is not null)
        {
            return IssueOperationsStaffBadgeResponse.Failure(refused);
        }

        if (await _users.GetAsync(request.UserId, cancellationToken) is not { } target || target.Account.Status == OperationsUserAccountStatus.Deactivated)
        {
            return IssueOperationsStaffBadgeResponse.Failure(OperationsCommonErrors.NotFound);
        }

        // The Owner's badge is the Owner's to print; an Administrator's, the Owner's or their own.
        if (target.IsOwner && target.Id != access!.User.Id)
        {
            return IssueOperationsStaffBadgeResponse.Failure(OperationsRoleErrors.OwnerProtected);
        }

        if (target.IsAdministrator && !access!.IsOwner && target.Id != access.User.Id)
        {
            return IssueOperationsStaffBadgeResponse.Failure(OperationsAccessErrors.OwnerOnly);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (await _quick.FindActiveBadgeAsync(target.Id, cancellationToken) is { } old)
        {
            old.Revoke(access!.User.Id, utcNow);
            _audit.Record(access.User.Id, "ops.staff.badge_revoked", nameof(OperationsUser), target.Id, arguments: new { replaced = true });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        string code = OperationsBadgeCode.New();
        _quick.Add(OperationsStaffBadge.Issue(context.RequiredTenantId, target.Id, _secrets.HashToken(code), access!.User.Id, utcNow));
        _audit.Record(access.User.Id, "ops.staff.badge_issued", nameof(OperationsUser), target.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? IssueOperationsStaffBadgeResponse.Success(code, target.FullName)
            : IssueOperationsStaffBadgeResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
