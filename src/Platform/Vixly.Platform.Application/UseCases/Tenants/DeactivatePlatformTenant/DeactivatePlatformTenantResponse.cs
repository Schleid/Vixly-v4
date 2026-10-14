using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.DeactivatePlatformTenant;

// =========================================================
// FILE: DeactivatePlatformTenantResponse.cs
//
// PURPOSE:
// Result of DeactivatePlatformTenant.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DeactivatePlatformTenantResponse
{
    private DeactivatePlatformTenantResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DeactivatePlatformTenantResponse Success()
    {
        return new DeactivatePlatformTenantResponse { Succeeded = true };
    }

    public static DeactivatePlatformTenantResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DeactivatePlatformTenantResponse { Error = error };
    }
}
