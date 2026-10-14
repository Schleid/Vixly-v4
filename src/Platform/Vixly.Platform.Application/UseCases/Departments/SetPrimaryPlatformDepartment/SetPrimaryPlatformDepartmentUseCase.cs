using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Platform.Application.UseCases.Users;

namespace Vixly.Platform.Application.UseCases.Departments.SetPrimaryPlatformDepartment;

// =========================================================
// FILE: SetPrimaryPlatformDepartmentUseCase.cs
//
// PURPOSE:
// Makes one of a staff user's departments their primary one.
//
// RESPONSIBILITIES:
// - Require platform.departments.manage_members
// - Refuse a department the user is not in
// - Audit the change
//
// IMPORTANT NOTES:
// - Exactly one primary department per user (domain rule)
// =========================================================
public sealed class SetPrimaryPlatformDepartmentUseCase
{
    private readonly PlatformUserAdministration _users;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SetPrimaryPlatformDepartmentUseCase(
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

    public async Task<SetPrimaryPlatformDepartmentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SetPrimaryPlatformDepartmentRequest request,
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
            return SetPrimaryPlatformDepartmentResponse.Failure(target.Error!);
        }

        try
        {
            user.SetPrimaryDepartment(request.PlatformDepartmentId, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return SetPrimaryPlatformDepartmentResponse.Failure(exception.Error);
        }

        _audit.Record(
            context.PlatformUserId,
            "platform.departments.primary_set",
            PlatformPermissions.DepartmentsManageMembers,
            nameof(PlatformUser),
            user.Id,
            arguments: new { departmentId = request.PlatformDepartmentId });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SetPrimaryPlatformDepartmentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return SetPrimaryPlatformDepartmentResponse.Success();
    }
}
