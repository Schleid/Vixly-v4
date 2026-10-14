using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.GetPlatformTenantEntitlements;

// =========================================================
// FILE: GetPlatformTenantEntitlementsResponse.cs
//
// PURPOSE:
// Reads what a tenant may use: access, features and effective limits.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformTenantEntitlementsResponse
{
    private GetPlatformTenantEntitlementsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantEntitlements? Entitlements { get; private init; }

    public static GetPlatformTenantEntitlementsResponse Success(PlatformTenantEntitlements? entitlements)
    {
        return new GetPlatformTenantEntitlementsResponse { Succeeded = true, Entitlements = entitlements };
    }

    public static GetPlatformTenantEntitlementsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformTenantEntitlementsResponse { Error = error };
    }
}
