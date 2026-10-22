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

namespace Vixly.Operations.Application.UseCases.Roles.ListOperationsRoles;

// =========================================================
// FILE: ListOperationsRolesUseCase.cs
//
// PURPOSE:
// Lists the roles of the tenant with their holders (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListOperationsRolesUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsRoleRepository _roles;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public ListOperationsRolesUseCase(
        OperationsAccess access,
        IOperationsRoleRepository roles,
        IOperationsUnitOfWork unitOfWork)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ListOperationsRolesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsRolesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.RolesView, false, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsRolesResponse.Failure(refused);
        }

        // The Administrator role exists from the first look, so it can be given.
        if (await _roles.FindAdministratorAsync(cancellationToken) is null)
        {
            await _access.AdministratorRoleAsync(context.RequiredTenantId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        IReadOnlyList<OperationsRoleGrant> grants = await _roles.ListActiveGrantsAsync(cancellationToken);
        IReadOnlyList<OperationsRole> roles = await _roles.ListAsync(cancellationToken);

        return ListOperationsRolesResponse.Success(roles
            .Select(role => new OperationsRoleSummary(role.Id, role.Name, role.IsAdministrator, role.IsActive, role.Permissions, grants.Count(grant => grant.RoleId == role.Id)))
            .ToList());
    }
}
