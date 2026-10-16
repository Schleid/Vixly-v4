using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.SetPlatformTenantTaxDetails;

// =========================================================
// FILE: SetPlatformTenantTaxDetailsResponse.cs
//
// PURPOSE:
// Sets a tenant's tax ID and tax exemption (D-B16).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetPlatformTenantTaxDetailsResponse
{
    private SetPlatformTenantTaxDetailsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetPlatformTenantTaxDetailsResponse Success()
    {
        return new SetPlatformTenantTaxDetailsResponse { Succeeded = true };
    }

    public static SetPlatformTenantTaxDetailsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetPlatformTenantTaxDetailsResponse { Error = error };
    }
}
