using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantAccount;

// =========================================================
// FILE: GetTenantAccountUseCase.cs
//
// PURPOSE:
// The tenant reads its own account and how to reach Vixly support (calls 15, 17, 18).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Any tenant role
// =========================================================
public sealed class GetTenantAccountUseCase
{
    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformBillingRepository _billing;

    public GetTenantAccountUseCase(
        IPlatformTenantRepository tenants,
        IPlatformBillingRepository billing)
    {
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    public async Task<GetTenantAccountResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        GetTenantAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenant? tenant = await _tenants.GetAsync(context.PlatformTenantId, cancellationToken);

        if (tenant is null)
        {
            return GetTenantAccountResponse.Failure(PlatformCommonErrors.NotFound);
        }

        return GetTenantAccountResponse.Success(PlatformTenantAccountSummary.From(tenant, await _billing.GetProfileAsync(cancellationToken)));
    }
}
