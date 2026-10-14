using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantProfile;

// =========================================================
// FILE: UpdatePlatformTenantProfileUseCase.cs
//
// PURPOSE:
// Changes a tenant's legal and contact details.
//
// RESPONSIBILITIES:
// - Check platform.tenants.edit
// - Validate and replace the company details
// - Audit before and after
//
// IMPORTANT NOTES:
// - The slug is not part of the profile (D-T11)
// =========================================================
public sealed class UpdatePlatformTenantProfileUseCase
{
    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public UpdatePlatformTenantProfileUseCase(
        PlatformTenantAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdatePlatformTenantProfileResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformTenantProfileRequest request,
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
            return UpdatePlatformTenantProfileResponse.Failure(target.Error!);
        }

        object before = Describe(tenant.Company);

        try
        {
            tenant.UpdateCompany(request.Company.ToDetails(), _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformTenantProfileResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, tenant, PlatformPermissions.TenantsEdit, "platform.tenants.profile_updated", before, Describe(tenant.Company));

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return UpdatePlatformTenantProfileResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return UpdatePlatformTenantProfileResponse.Success();
    }

    private static object Describe(PlatformCompanyDetails company)
    {
        return new
        {
            legalName = company.LegalName,
            tradingName = company.TradingName,
            email = company.Email.Value,
            phone = company.Phone.Value,
            city = company.Address.City,
            country = company.Address.CountryCode
        };
    }
}
