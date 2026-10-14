using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.ActivatePlatformTenant;

// =========================================================
// FILE: ActivatePlatformTenantResponse.cs
//
// PURPOSE:
// Result of ActivatePlatformTenant.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ActivatePlatformTenantResponse
{
    private ActivatePlatformTenantResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ActivatePlatformTenantResponse Success()
    {
        return new ActivatePlatformTenantResponse { Succeeded = true };
    }

    public static ActivatePlatformTenantResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ActivatePlatformTenantResponse { Error = error };
    }
}
