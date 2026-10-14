using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Departments.ReactivatePlatformDepartment;

// =========================================================
// FILE: ReactivatePlatformDepartmentUseCase.cs
//
// PURPOSE:
// Reactivates a department.
//
// RESPONSIBILITIES:
// - Require platform.departments.deactivate
// - Audit the change
//
// IMPORTANT NOTES:
// - Members kept while it was inactive are still there
// =========================================================
public sealed class ReactivatePlatformDepartmentUseCase
{
    private readonly PlatformDepartmentAdministration _administration;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ReactivatePlatformDepartmentUseCase(
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

    public async Task<ReactivatePlatformDepartmentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ReactivatePlatformDepartmentRequest request,
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
            return ReactivatePlatformDepartmentResponse.Failure(target.Error!);
        }

        try
        {
            department.Reactivate(_timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return ReactivatePlatformDepartmentResponse.Failure(exception.Error);
        }

        _audit.Record(context.PlatformUserId, "platform.departments.reactivated", PlatformPermissions.DepartmentsDeactivate, nameof(PlatformDepartment), department.Id);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ReactivatePlatformDepartmentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return ReactivatePlatformDepartmentResponse.Success();
    }
}
