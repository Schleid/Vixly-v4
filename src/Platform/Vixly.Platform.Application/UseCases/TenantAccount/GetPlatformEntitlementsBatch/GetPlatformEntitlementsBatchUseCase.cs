using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetPlatformEntitlementsBatch;

// =========================================================
// FILE: GetPlatformEntitlementsBatchUseCase.cs
//
// PURPOSE:
// Entitlements for up to 100 tenants in one call, for Operations background jobs (D-K6).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations with the jobs scope; no tenant in the token
// - An unknown tenant answers no status and no access, like a tenant with no subscription
// =========================================================
public sealed class GetPlatformEntitlementsBatchUseCase
{
    private readonly PlatformEntitlementsReader _reader;

    public GetPlatformEntitlementsBatchUseCase(
        PlatformEntitlementsReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<GetPlatformEntitlementsBatchResponse> ExecuteAsync(
        PlatformInternalAuthorizationContext context,
        GetPlatformEntitlementsBatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Guid[] ids = request.PlatformTenantIds.Where(id => id != Guid.Empty).Distinct().ToArray();

        if (ids.Length > PlatformTenantAccountLimits.BatchMax)
        {
            return GetPlatformEntitlementsBatchResponse.Failure(PlatformTenantAccountErrors.TooManyTenants);
        }

        List<PlatformTenantEntitlements> items = new(ids.Length);

        foreach (Guid id in ids)
        {
            items.Add(await _reader.ReadAsync(id, cancellationToken));
        }

        return GetPlatformEntitlementsBatchResponse.Success(items);
    }
}
