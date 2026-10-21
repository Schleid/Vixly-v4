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

namespace Vixly.Operations.Application.UseCases.Roles.GrantOperationsRole;

// =========================================================
// FILE: GrantOperationsRoleUseCase.cs
//
// PURPOSE:
// Gives a staff member a role for the tenant, a branch or a warehouse (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Step-up; a granter gives only what they hold in that scope; the Administrator role only by the Owner
// - An Administrator must enrol a second factor at the next sign-in (D-O2)
// =========================================================
public sealed class GrantOperationsRoleUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsRoleRepository _roles;

    private readonly IOperationsLocationRepository _locations;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public GrantOperationsRoleUseCase(
        OperationsAccess access,
        IOperationsUserRepository users,
        IOperationsRoleRepository roles,
        IOperationsLocationRepository locations,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GrantOperationsRoleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GrantOperationsRoleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.RolesGrant, true, cancellationToken);

        if (refused is not null)
        {
            return GrantOperationsRoleResponse.Failure(refused);
        }

        OperationsUser? user = await _users.GetAsync(request.UserId, cancellationToken);
        OperationsRole? role = await _roles.GetAsync(request.RoleId, cancellationToken);

        if (user is null || role is null)
        {
            return GrantOperationsRoleResponse.Failure(OperationsCommonErrors.NotFound);
        }

        if (user.Id == access!.User.Id)
        {
            return GrantOperationsRoleResponse.Failure(OperationsRoleErrors.NotYourself);
        }

        if (user.IsOwner)
        {
            return GrantOperationsRoleResponse.Failure(OperationsRoleErrors.OwnerProtected);
        }

        if (!role.IsActive)
        {
            return GrantOperationsRoleResponse.Failure(OperationsRoleErrors.RoleInactive);
        }

        Guid? branchOfWarehouse = null;

        if (request.Scope == OperationsRoleScope.Branch && (request.ScopeId is not { } branchId || await _locations.GetBranchAsync(branchId, cancellationToken) is null))
        {
            return GrantOperationsRoleResponse.Failure(OperationsRoleErrors.ScopeInvalid);
        }

        if (request.Scope == OperationsRoleScope.Warehouse)
        {
            branchOfWarehouse = request.ScopeId is { } warehouseId ? (await _locations.GetWarehouseAsync(warehouseId, cancellationToken))?.BranchId : null;

            if (branchOfWarehouse is null)
            {
                return GrantOperationsRoleResponse.Failure(OperationsRoleErrors.ScopeInvalid);
            }
        }

        // The Administrator role is tenant-wide and given only by the Owner.
        if (role.IsAdministrator && request.Scope != OperationsRoleScope.Tenant)
        {
            return GrantOperationsRoleResponse.Failure(OperationsRoleErrors.ScopeInvalid);
        }

        if (!access.CanGrant(role, request.Scope, request.ScopeId, branchOfWarehouse))
        {
            return GrantOperationsRoleResponse.Failure(role.IsAdministrator ? OperationsAccessErrors.OwnerOnly : OperationsAccessErrors.PermissionRequired);
        }

        if ((await _roles.ListGrantsAsync(user.Id, cancellationToken)).Any(grant => grant.IsActive && grant.RoleId == role.Id && grant.Scope == request.Scope && grant.ScopeId == request.ScopeId))
        {
            return GrantOperationsRoleResponse.Failure(OperationsRoleErrors.GrantExists);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsRoleGrant created;

        try
        {
            created = OperationsRoleGrant.Grant(context.RequiredTenantId, user.Id, role.Id, request.Scope, request.ScopeId, access.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return GrantOperationsRoleResponse.Failure(exception.Error);
        }

        _roles.Add(created);

        if (role.IsAdministrator)
        {
            user.SetAdministrator(true, utcNow);
        }

        _audit.Record(access.User.Id, "ops.roles.granted", nameof(OperationsUser), user.Id, arguments: new { role = role.Id, scope = request.Scope.ToString(), scopeId = request.ScopeId });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? GrantOperationsRoleResponse.Success(created.Id)
            : GrantOperationsRoleResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
