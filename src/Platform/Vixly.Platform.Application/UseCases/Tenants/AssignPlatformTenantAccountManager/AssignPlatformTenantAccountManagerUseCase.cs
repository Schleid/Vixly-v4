using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.AssignPlatformTenantAccountManager;

// =========================================================
// FILE: AssignPlatformTenantAccountManagerUseCase.cs
//
// PURPOSE:
// Sets or clears the staff member who manages a tenant (D-T16).
//
// RESPONSIBILITIES:
// - Check platform.tenants.edit
// - Accept only an active Platform user
// - Audit before and after
//
// IMPORTANT NOTES:
// - Null clears the account manager
// =========================================================
public sealed class AssignPlatformTenantAccountManagerUseCase
{
    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public AssignPlatformTenantAccountManagerUseCase(
        PlatformTenantAdministration administration,
        IPlatformUserRepository users,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<AssignPlatformTenantAccountManagerResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        AssignPlatformTenantAccountManagerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenantTarget target = await _administration.LoadTenantAsync(
            context,
            PlatformPermissions.TenantsEdit,
            request.PlatformTenantId,
            requireStepUp: false,
            cancellationToken);

        if (target.Tenant is not PlatformTenant tenant)
        {
            return AssignPlatformTenantAccountManagerResponse.Failure(target.Error!);
        }

        if (request.PlatformUserId is Guid managerId
            && await _users.GetAsync(managerId, cancellationToken) is not { Account.IsActive: true })
        {
            return AssignPlatformTenantAccountManagerResponse.Failure(PlatformTenantErrors.AccountManagerInvalid);
        }

        Guid? before = tenant.AccountManagerUserId;

        try
        {
            tenant.AssignAccountManager(request.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return AssignPlatformTenantAccountManagerResponse.Failure(exception.Error);
        }

        _administration.RecordChange(
            context,
            tenant,
            PlatformPermissions.TenantsEdit,
            "platform.tenants.account_manager_assigned",
            new { accountManager = before },
            new { accountManager = request.PlatformUserId });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return AssignPlatformTenantAccountManagerResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return AssignPlatformTenantAccountManagerResponse.Success();
    }
}
