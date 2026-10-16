using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantEntitlements;

// =========================================================
// FILE: GetTenantEntitlementsUseCase.cs
//
// PURPOSE:
// The tenant's entitlements: status, access, features, limits, non-payment and legal parts (call 14).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Any tenant role; cached by Operations for at most 5 minutes (D-K6)
// =========================================================
public sealed class GetTenantEntitlementsUseCase
{
    private readonly PlatformEntitlementsReader _reader;

    public GetTenantEntitlementsUseCase(
        PlatformEntitlementsReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<GetTenantEntitlementsResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        GetTenantEntitlementsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        return GetTenantEntitlementsResponse.Success(await _reader.ReadAsync(context.PlatformTenantId, cancellationToken));
    }
}
