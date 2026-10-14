using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Platform.Application.UseCases.Users;

namespace Vixly.Platform.Application.UseCases.Departments.AddPlatformDepartmentMember;

// =========================================================
// FILE: AddPlatformDepartmentMemberUseCase.cs
//
// PURPOSE:
// Puts a staff user in a department.
//
// RESPONSIBILITIES:
// - Require platform.departments.manage_members
// - Refuse an inactive department and a target with a higher
//   authority level
// - Audit the change
//
// IMPORTANT NOTES:
// - The user's first department becomes their primary one
// - No step-up: departments grant nothing (decision D-S3)
// =========================================================
public sealed class AddPlatformDepartmentMemberUseCase
{
    private readonly PlatformUserAdministration _users;

    private readonly IPlatformDepartmentRepository _departments;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AddPlatformDepartmentMemberUseCase(
        PlatformUserAdministration users,
        IPlatformDepartmentRepository departments,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _departments = departments ?? throw new ArgumentNullException(nameof(departments));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AddPlatformDepartmentMemberResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        AddPlatformDepartmentMemberRequest request,
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
            return AddPlatformDepartmentMemberResponse.Failure(target.Error!);
        }

        PlatformDepartment? department = await _departments.GetAsync(request.PlatformDepartmentId, cancellationToken);

        if (department is null)
        {
            return AddPlatformDepartmentMemberResponse.Failure(PlatformCommonErrors.NotFound);
        }

        if (!department.IsActive)
        {
            return AddPlatformDepartmentMemberResponse.Failure(PlatformDepartmentErrors.DepartmentInactive);
        }

        try
        {
            user.JoinDepartment(department.Id, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return AddPlatformDepartmentMemberResponse.Failure(exception.Error);
        }

        _audit.Record(
            context.PlatformUserId,
            "platform.departments.member_added",
            PlatformPermissions.DepartmentsManageMembers,
            nameof(PlatformUser),
            user.Id,
            arguments: new { departmentId = request.PlatformDepartmentId });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return AddPlatformDepartmentMemberResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return AddPlatformDepartmentMemberResponse.Success();
    }
}
