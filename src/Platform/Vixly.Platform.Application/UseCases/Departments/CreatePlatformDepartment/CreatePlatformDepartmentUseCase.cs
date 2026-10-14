using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Departments.CreatePlatformDepartment;

// =========================================================
// FILE: CreatePlatformDepartmentUseCase.cs
//
// PURPOSE:
// Creates a department.
//
// RESPONSIBILITIES:
// - Require platform.departments.create
// - Refuse a name another department already uses
// - Audit the creation
//
// IMPORTANT NOTES:
// - A department name is not translated (L-5)
// =========================================================
public sealed class CreatePlatformDepartmentUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformDepartmentRepository _departments;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CreatePlatformDepartmentUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformDepartmentRepository departments,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _departments = departments ?? throw new ArgumentNullException(nameof(departments));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CreatePlatformDepartmentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CreatePlatformDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.DepartmentsCreate, cancellationToken);

        PlatformDepartment department;

        try
        {
            department = PlatformDepartment.Create(request.Name, request.Description, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return CreatePlatformDepartmentResponse.Failure(exception.Error);
        }

        if (await _departments.NameExistsAsync(department.Name, null, cancellationToken))
        {
            return CreatePlatformDepartmentResponse.Failure(PlatformDepartmentErrors.NameAlreadyUsed);
        }

        _departments.Add(department);
        _audit.Record(
            context.PlatformUserId,
            "platform.departments.created",
            PlatformPermissions.DepartmentsCreate,
            nameof(PlatformDepartment),
            department.Id,
            after: new { name = department.Name, description = department.Description });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CreatePlatformDepartmentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return CreatePlatformDepartmentResponse.Success(department.Id);
    }
}
