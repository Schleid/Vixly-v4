using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.SetPlatformTenantTaxDetails;

// =========================================================
// FILE: SetPlatformTenantTaxDetailsUseCase.cs
//
// PURPOSE:
// Sets a tenant's tax ID and tax exemption (D-B16).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SetPlatformTenantTaxDetailsUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public SetPlatformTenantTaxDetailsUseCase(
        PlatformBillingAdministration billing,
        IPlatformTenantRepository tenants,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SetPlatformTenantTaxDetailsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SetPlatformTenantTaxDetailsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.TenantsEdit, requireStepUp: false, cancellationToken) is { } refused)
        {
            return SetPlatformTenantTaxDetailsResponse.Failure(refused);
        }

        var tenant = await _tenants.GetAsync(request.PlatformTenantId, cancellationToken);

        if (tenant is null)
        {
            return SetPlatformTenantTaxDetailsResponse.Failure(PlatformCommonErrors.NotFound);
        }

        try
        {
            tenant.SetTaxDetails(request.TaxId, request.TaxExempt, request.ExemptionReason, request.ExemptionReference, _billing.UtcNow);
        }
        catch (DomainException exception)
        {
            return SetPlatformTenantTaxDetailsResponse.Failure(exception.Error);
        }

        _billing.Audit(context, "platform.billing.tenant_tax_updated", PlatformPermissions.TenantsEdit, "PlatformTenant", tenant.Id,
            new { taxId = tenant.TaxId, exempt = tenant.TaxExempt }, tenant.TaxExemptionReason);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SetPlatformTenantTaxDetailsResponse.Success()
            : SetPlatformTenantTaxDetailsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
