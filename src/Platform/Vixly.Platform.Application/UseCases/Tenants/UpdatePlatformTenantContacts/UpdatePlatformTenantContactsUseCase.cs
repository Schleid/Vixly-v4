using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantContacts;

// =========================================================
// FILE: UpdatePlatformTenantContactsUseCase.cs
//
// PURPOSE:
// Replaces a tenant's named contacts (D-T15).
//
// RESPONSIBILITIES:
// - Check platform.tenants.edit
// - Validate one owner and unique roles
// - Audit before and after
//
// IMPORTANT NOTES:
// - Billing and Technical fall back to Owner when absent
// =========================================================
public sealed class UpdatePlatformTenantContactsUseCase
{
    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public UpdatePlatformTenantContactsUseCase(
        PlatformTenantAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdatePlatformTenantContactsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformTenantContactsRequest request,
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
            return UpdatePlatformTenantContactsResponse.Failure(target.Error!);
        }

        object before = Describe(tenant);

        try
        {
            tenant.ReplaceContacts(request.Contacts.Select(contact => contact.ToDetails()).ToList(), _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformTenantContactsResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, tenant, PlatformPermissions.TenantsEdit, "platform.tenants.contacts_updated", before, Describe(tenant));

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return UpdatePlatformTenantContactsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return UpdatePlatformTenantContactsResponse.Success();
    }

    private static object Describe(PlatformTenant tenant)
    {
        return tenant.Contacts
            .OrderBy(contact => contact.Role)
            .Select(contact => new { role = contact.Role.ToString(), contact.Name, email = contact.Email.Value })
            .ToArray();
    }
}
