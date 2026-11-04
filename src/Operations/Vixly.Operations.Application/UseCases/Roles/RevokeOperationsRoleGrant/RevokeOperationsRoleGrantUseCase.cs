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

namespace Vixly.Operations.Application.UseCases.Roles.RevokeOperationsRoleGrant;

// =========================================================
// FILE: RevokeOperationsRoleGrantUseCase.cs
//
// PURPOSE:
// Takes a role back from a staff member (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class RevokeOperationsRoleGrantUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsRoleRepository _roles;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RevokeOperationsRoleGrantUseCase(
        OperationsAccess access,
        IOperationsUserRepository users,
        IOperationsRoleRepository roles,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RevokeOperationsRoleGrantResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RevokeOperationsRoleGrantRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.RolesGrant, true, cancellationToken);

        if (refused is not null)
        {
            return RevokeOperationsRoleGrantResponse.Failure(refused);
        }

        OperationsRoleGrant? grant = await _roles.GetGrantAsync(request.GrantId, cancellationToken);
        OperationsRole? role = grant is null ? null : await _roles.GetAsync(grant.RoleId, cancellationToken);
        OperationsUser? user = grant is null ? null : await _users.GetAsync(grant.UserId, cancellationToken);

        if (grant is null || role is null || user is null || !grant.IsActive || (request.UserId is { } expected && grant.UserId != expected))
        {
            return RevokeOperationsRoleGrantResponse.Failure(OperationsCommonErrors.NotFound);
        }

        if (user.Id == access!.User.Id)
        {
            return RevokeOperationsRoleGrantResponse.Failure(OperationsRoleErrors.NotYourself);
        }

        if (role.IsAdministrator && !access.IsOwner)
        {
            return RevokeOperationsRoleGrantResponse.Failure(OperationsAccessErrors.OwnerOnly);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        grant.Revoke(access.User.Id, utcNow);

        if (role.IsAdministrator)
        {
            user.SetAdministrator(false, utcNow);
        }

        _audit.Record(access.User.Id, "ops.roles.revoked", nameof(OperationsUser), user.Id, arguments: new { role = role.Id, grant = grant.Id });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RevokeOperationsRoleGrantResponse.Success()
            : RevokeOperationsRoleGrantResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
