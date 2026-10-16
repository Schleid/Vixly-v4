using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.GetPlatformTenantBilling;

// =========================================================
// FILE: GetPlatformTenantBillingUseCase.cs
//
// PURPOSE:
// Reads a tenant's open balance, tax details and the tax rate that applies (tenant page).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformTenantBillingUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformBillingQueries _queries;

    private readonly IPlatformTenantRepository _tenants;

    public GetPlatformTenantBillingUseCase(
        PlatformBillingAdministration billing,
        IPlatformBillingQueries queries,
        IPlatformTenantRepository tenants)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
    }

    public async Task<GetPlatformTenantBillingResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformTenantBillingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformTenantBillingResponse.Failure(refused);
        }

        var tenant = await _tenants.GetAsync(request.PlatformTenantId, cancellationToken);

        if (tenant is null)
        {
            return GetPlatformTenantBillingResponse.Failure(PlatformCommonErrors.NotFound);
        }

        (decimal rate, string? label) = await _billing.TaxForAsync(tenant, cancellationToken);

        return GetPlatformTenantBillingResponse.Success(new PlatformTenantBillingDetail(
            await _queries.SummaryAsync(tenant.Id, _billing.UtcNow, cancellationToken),
            tenant.TaxId,
            tenant.TaxExempt,
            tenant.TaxExemptionReason,
            tenant.TaxExemptionReference,
            tenant.Company.Address.CountryCode,
            tenant.Company.Address.StateProvince,
            rate,
            label,
            await _billing.HoldsAsync(context, PlatformPermissions.TenantsEdit, cancellationToken),
            await _billing.HoldsAsync(context, PlatformPermissions.SubscriptionsCharges, cancellationToken)));
    }
}
