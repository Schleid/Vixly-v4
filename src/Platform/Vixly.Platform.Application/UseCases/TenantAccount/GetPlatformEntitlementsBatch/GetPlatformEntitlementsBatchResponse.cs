using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetPlatformEntitlementsBatch;

// =========================================================
// FILE: GetPlatformEntitlementsBatchResponse.cs
//
// PURPOSE:
// Entitlements for up to 100 tenants in one call, for Operations background jobs (D-K6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformEntitlementsBatchResponse
{
    private GetPlatformEntitlementsBatchResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformTenantEntitlements> Items { get; private init; } = Array.Empty<PlatformTenantEntitlements>();

    public static GetPlatformEntitlementsBatchResponse Success(IReadOnlyList<PlatformTenantEntitlements> items)
    {
        return new GetPlatformEntitlementsBatchResponse { Succeeded = true, Items = items };
    }

    public static GetPlatformEntitlementsBatchResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformEntitlementsBatchResponse { Error = error };
    }
}
