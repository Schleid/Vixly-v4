using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.GetPlatformTenantBilling;

// =========================================================
// FILE: GetPlatformTenantBillingResponse.cs
//
// PURPOSE:
// Reads a tenant's open balance, tax details and the tax rate that applies (tenant page).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformTenantBillingResponse
{
    private GetPlatformTenantBillingResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantBillingDetail? Billing { get; private init; }

    public static GetPlatformTenantBillingResponse Success(PlatformTenantBillingDetail? billing)
    {
        return new GetPlatformTenantBillingResponse { Succeeded = true, Billing = billing };
    }

    public static GetPlatformTenantBillingResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformTenantBillingResponse { Error = error };
    }
}
