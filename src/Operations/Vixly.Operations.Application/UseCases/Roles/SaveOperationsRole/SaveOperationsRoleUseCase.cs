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

namespace Vixly.Operations.Application.UseCases.Roles.SaveOperationsRole;

// =========================================================
// FILE: SaveOperationsRoleUseCase.cs
//
// PURPOSE:
// Creates a role or changes its name and permissions (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Changing the permissions of an existing role needs step-up
// - The Administrator role cannot be changed; system-only permissions are refused (domain)
// =========================================================
public sealed class SaveOperationsRoleUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsRoleRepository _roles;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsRoleUseCase(
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

    public async Task<SaveOperationsRoleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsRoleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsRole? role = request.RoleId is { } id ? await _roles.GetAsync(id, cancellationToken) : null;

        if (request.RoleId is not null && role is null)
        {
            return SaveOperationsRoleResponse.Failure(OperationsCommonErrors.NotFound);
        }

        bool permissionsChange = role is null || !role.Permissions.OrderBy(code => code).SequenceEqual(request.Permissions.Distinct().OrderBy(code => code));
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context,
            role is null ? OperationsPermissions.RolesCreate : permissionsChange ? OperationsPermissions.RolesEditPermissions : OperationsPermissions.RolesEdit,
            role is not null && permissionsChange, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsRoleResponse.Failure(refused);
        }

        // Nobody puts in a role a permission they do not hold for the whole tenant (design section 3).
        if (request.Permissions.Any(permission => !access!.HasTenantWide(permission)))
        {
            return SaveOperationsRoleResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if ((await _roles.ListAsync(cancellationToken)).Any(other => other.Id != role?.Id && string.Equals(other.Name, request.Name?.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return SaveOperationsRoleResponse.Failure(OperationsRoleErrors.NameTaken);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            if (role is null)
            {
                role = OperationsRole.Create(context.RequiredTenantId, request.Name, request.Permissions, utcNow);
                _roles.Add(role);
            }
            else
            {
                role.Change(request.Name, request.Permissions, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsRoleResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, request.RoleId is null ? "ops.roles.created" : "ops.roles.changed", nameof(OperationsRole), role.Id, after: new { name = role.Name, permissions = role.Permissions });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsRoleResponse.Success(role.Id)
            : SaveOperationsRoleResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
