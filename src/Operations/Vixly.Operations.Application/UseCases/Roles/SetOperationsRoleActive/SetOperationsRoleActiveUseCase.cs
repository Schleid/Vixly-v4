using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Roles.SetOperationsRoleActive;

// =========================================================
// FILE: SetOperationsRoleActiveUseCase.cs
//
// PURPOSE:
// Deactivates or reactivates a role (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Reactivating needs step-up (it gives permissions back at once)
// =========================================================
public sealed class SetOperationsRoleActiveUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsRoleRepository _roles;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SetOperationsRoleActiveUseCase(
        OperationsAccess access,
        IOperationsRoleRepository roles,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SetOperationsRoleActiveResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SetOperationsRoleActiveRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.RolesEdit, request.Active, cancellationToken);

        if (refused is not null)
        {
            return SetOperationsRoleActiveResponse.Failure(refused);
        }

        OperationsRole? role = await _roles.GetAsync(request.RoleId, cancellationToken);

        if (role is null)
        {
            return SetOperationsRoleActiveResponse.Failure(OperationsCommonErrors.NotFound);
        }

        if (request.Active && !role.IsAdministrator && !access!.CanGrant(role, OperationsRoleScope.Tenant, null, null))
        {
            return SetOperationsRoleActiveResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        try
        {
            role.SetActive(request.Active, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return SetOperationsRoleActiveResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, request.Active ? "ops.roles.reactivated" : "ops.roles.deactivated", nameof(OperationsRole), role.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SetOperationsRoleActiveResponse.Success()
            : SetOperationsRoleActiveResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
