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

namespace Vixly.Operations.Application.UseCases.Staff.ResetOperationsStaffSecondFactor;

// =========================================================
// FILE: ResetOperationsStaffSecondFactorUseCase.cs
//
// PURPOSE:
// Removes a staff member's authenticator app and recovery codes so they enrol again (Slice 1D, D-O14).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The Owner resets an Administrator's; the Owner's own goes to Vixly support (D-O13)
// - Every session of that person ends
// =========================================================
public sealed class ResetOperationsStaffSecondFactorUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsSessionRepository _sessions;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ResetOperationsStaffSecondFactorUseCase(
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

    public async Task<ResetOperationsStaffSecondFactorResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ResetOperationsStaffSecondFactorRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.StaffResetSecondFactor, true, cancellationToken);

        if (refused is not null)
        {
            return ResetOperationsStaffSecondFactorResponse.Failure(refused);
        }

        OperationsUser? target = await _users.GetAsync(request.UserId, cancellationToken);

        if (target is null)
        {
            return ResetOperationsStaffSecondFactorResponse.Failure(OperationsCommonErrors.NotFound);
        }

        // Nobody acts on their own account here; the Owner is protected; only the Owner acts on an Administrator.
        if (target.Id == access!.User.Id)
        {
            return ResetOperationsStaffSecondFactorResponse.Failure(OperationsRoleErrors.NotYourself);
        }

        if (target.IsOwner)
        {
            return ResetOperationsStaffSecondFactorResponse.Failure(OperationsRoleErrors.OwnerProtected);
        }

        if (target.IsAdministrator && !access.IsOwner)
        {
            return ResetOperationsStaffSecondFactorResponse.Failure(OperationsAccessErrors.OwnerOnly);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        target.ResetSecondFactor(utcNow);

        foreach (OperationsUserSession session in await _sessions.ListOpenAsync(target.Id, cancellationToken))
        {
            session.End(OperationsSessionEndReason.SecondFactorReset, utcNow);
        }

        _audit.Record(access.User.Id, "ops.staff.second_factor_reset", nameof(OperationsUser), target.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ResetOperationsStaffSecondFactorResponse.Success()
            : ResetOperationsStaffSecondFactorResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
