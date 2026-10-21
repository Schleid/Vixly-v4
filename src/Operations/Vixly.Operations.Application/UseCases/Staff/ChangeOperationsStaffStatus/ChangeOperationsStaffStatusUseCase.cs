using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Staff.ChangeOperationsStaffStatus;

// =========================================================
// FILE: ChangeOperationsStaffStatusUseCase.cs
//
// PURPOSE:
// Suspends, locks, deactivates or reactivates a staff member, with a reason (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Step-up; never on oneself; the Owner is protected; only the Owner acts on an Administrator
// =========================================================
public sealed class ChangeOperationsStaffStatusUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsSessionRepository _sessions;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsStaffStatusUseCase(
        OperationsAccess access,
        IOperationsUserRepository users,
        IOperationsSessionRepository sessions,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsStaffStatusResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsStaffStatusRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        string permission = request.Status switch
        {
            OperationsUserAccountStatus.Suspended => OperationsPermissions.StaffSuspend,
            OperationsUserAccountStatus.Locked => OperationsPermissions.StaffLock,
            OperationsUserAccountStatus.Deactivated => OperationsPermissions.StaffDeactivate,
            _ => OperationsPermissions.StaffReactivate
        };

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, permission, true, cancellationToken);

        if (refused is not null)
        {
            return ChangeOperationsStaffStatusResponse.Failure(refused);
        }

        OperationsUser? target = await _users.GetAsync(request.UserId, cancellationToken);

        if (target is null)
        {
            return ChangeOperationsStaffStatusResponse.Failure(OperationsCommonErrors.NotFound);
        }

        // Nobody acts on their own account here; the Owner is protected; only the Owner acts on an Administrator.
        if (target.Id == access!.User.Id)
        {
            return ChangeOperationsStaffStatusResponse.Failure(OperationsRoleErrors.NotYourself);
        }

        if (target.IsOwner)
        {
            return ChangeOperationsStaffStatusResponse.Failure(OperationsRoleErrors.OwnerProtected);
        }

        if (target.IsAdministrator && !access.IsOwner)
        {
            return ChangeOperationsStaffStatusResponse.Failure(OperationsAccessErrors.OwnerOnly);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            target.ChangeStatus(request.Status, request.Reason, utcNow);
        }
        catch (DomainException exception)
        {
            return ChangeOperationsStaffStatusResponse.Failure(exception.Error);
        }

        // Suspend, lock and deactivate end every session at once (design section 3).
        if (request.Status != OperationsUserAccountStatus.Active)
        {
            foreach (OperationsUserSession session in await _sessions.ListOpenAsync(target.Id, cancellationToken))
            {
                session.End(OperationsSessionEndReason.AccountRestricted, utcNow);
            }
        }

        _audit.Record(access.User.Id, "ops.staff.status_changed", nameof(OperationsUser), target.Id, arguments: new { status = request.Status.ToString(), reason = request.Reason });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsStaffStatusResponse.Success()
            : ChangeOperationsStaffStatusResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
