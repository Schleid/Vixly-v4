using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Departments.UpdatePlatformDepartment;

// =========================================================
// FILE: UpdatePlatformDepartmentUseCase.cs
//
// PURPOSE:
// Changes the name and description of a department.
//
// RESPONSIBILITIES:
// - Require platform.departments.edit
// - Refuse a name another department uses
// - Audit the values before and after
//
// IMPORTANT NOTES:
// - Works on inactive departments too
// =========================================================
public sealed class UpdatePlatformDepartmentUseCase
{
    private readonly PlatformDepartmentAdministration _administration;

    private readonly IPlatformDepartmentRepository _departments;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public UpdatePlatformDepartmentUseCase(
        PlatformDepartmentAdministration administration,
        IPlatformDepartmentRepository departments,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _departments = departments ?? throw new ArgumentNullException(nameof(departments));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<UpdatePlatformDepartmentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformDepartmentTarget target = await _administration.LoadDepartmentAsync(
            context,
            PlatformPermissions.DepartmentsEdit,
            request.PlatformDepartmentId,
            cancellationToken);

        if (target.Department is not PlatformDepartment department)
        {
            return UpdatePlatformDepartmentResponse.Failure(target.Error!);
        }

        var before = new { name = department.Name, description = department.Description };

        try
        {
            department.Update(request.Name, request.Description, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return UpdatePlatformDepartmentResponse.Failure(exception.Error);
        }

        if (before.name == department.Name && before.description == department.Description)
        {
            return UpdatePlatformDepartmentResponse.Success();
        }

        if (await _departments.NameExistsAsync(department.Name, department.Id, cancellationToken))
        {
            return UpdatePlatformDepartmentResponse.Failure(PlatformDepartmentErrors.NameAlreadyUsed);
        }

        _audit.Record(
            context.PlatformUserId,
            "platform.departments.updated",
            PlatformPermissions.DepartmentsEdit,
            nameof(PlatformDepartment),
            department.Id,
            before: before,
            after: new { name = department.Name, description = department.Description });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return UpdatePlatformDepartmentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return UpdatePlatformDepartmentResponse.Success();
    }
}
