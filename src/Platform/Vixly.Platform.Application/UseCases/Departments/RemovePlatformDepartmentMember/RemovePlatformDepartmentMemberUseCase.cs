using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Platform.Application.UseCases.Users;

namespace Vixly.Platform.Application.UseCases.Departments.RemovePlatformDepartmentMember;

// =========================================================
// FILE: RemovePlatformDepartmentMemberUseCase.cs
//
// PURPOSE:
// Takes a staff user out of a department.
//
// RESPONSIBILITIES:
// - Require platform.departments.manage_members
// - Refuse a target with a higher authority level
// - Audit the change
//
// IMPORTANT NOTES:
// - When the primary department is removed, the next one
//   becomes primary
// =========================================================
public sealed class RemovePlatformDepartmentMemberUseCase
{
    private readonly PlatformUserAdministration _users;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RemovePlatformDepartmentMemberUseCase(
        PlatformUserAdministration users,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RemovePlatformDepartmentMemberResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RemovePlatformDepartmentMemberRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformUserTarget target = await _users.LoadTargetAsync(
            context,
            PlatformPermissions.DepartmentsManageMembers,
            request.PlatformUserId,
            requireStepUp: false,
            cancellationToken);

        if (target.User is not PlatformUser user)
        {
            return RemovePlatformDepartmentMemberResponse.Failure(target.Error!);
        }

        try
        {
            user.LeaveDepartment(request.PlatformDepartmentId, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return RemovePlatformDepartmentMemberResponse.Failure(exception.Error);
        }

        _audit.Record(
            context.PlatformUserId,
            "platform.departments.member_removed",
            PlatformPermissions.DepartmentsManageMembers,
            nameof(PlatformUser),
            user.Id,
            arguments: new { departmentId = request.PlatformDepartmentId });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RemovePlatformDepartmentMemberResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return RemovePlatformDepartmentMemberResponse.Success();
    }
}
