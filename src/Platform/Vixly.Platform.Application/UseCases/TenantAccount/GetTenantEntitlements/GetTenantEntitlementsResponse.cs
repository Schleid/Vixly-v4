using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantEntitlements;

// =========================================================
// FILE: GetTenantEntitlementsResponse.cs
//
// PURPOSE:
// The tenant's entitlements: status, access, features, limits, non-payment and legal parts (call 14).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetTenantEntitlementsResponse
{
    private GetTenantEntitlementsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantEntitlements? Entitlements { get; private init; }

    public static GetTenantEntitlementsResponse Success(PlatformTenantEntitlements? entitlements)
    {
        return new GetTenantEntitlementsResponse { Succeeded = true, Entitlements = entitlements };
    }

    public static GetTenantEntitlementsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetTenantEntitlementsResponse { Error = error };
    }
}
