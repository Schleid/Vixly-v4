using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Departments.DeactivatePlatformDepartment;

// =========================================================
// FILE: DeactivatePlatformDepartmentUseCase.cs
//
// PURPOSE:
// Deactivates a department.
//
// RESPONSIBILITIES:
// - Require platform.departments.deactivate
// - Audit the change
//
// IMPORTANT NOTES:
// - Members stay in it; nobody can be added until it is
//   reactivated
// =========================================================
public sealed class DeactivatePlatformDepartmentUseCase
{
    private readonly PlatformDepartmentAdministration _administration;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public DeactivatePlatformDepartmentUseCase(
        PlatformDepartmentAdministration administration,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DeactivatePlatformDepartmentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        DeactivatePlatformDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformDepartmentTarget target = await _administration.LoadDepartmentAsync(
            context,
            PlatformPermissions.DepartmentsDeactivate,
            request.PlatformDepartmentId,
            cancellationToken);

        if (target.Department is not PlatformDepartment department)
        {
            return DeactivatePlatformDepartmentResponse.Failure(target.Error!);
        }

        try
        {
            department.Deactivate(_timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return DeactivatePlatformDepartmentResponse.Failure(exception.Error);
        }

        _audit.Record(context.PlatformUserId, "platform.departments.deactivated", PlatformPermissions.DepartmentsDeactivate, nameof(PlatformDepartment), department.Id);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return DeactivatePlatformDepartmentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return DeactivatePlatformDepartmentResponse.Success();
    }
}
