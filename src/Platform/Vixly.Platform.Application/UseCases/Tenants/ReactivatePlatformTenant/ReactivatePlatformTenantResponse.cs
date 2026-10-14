using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.ReactivatePlatformTenant;

// =========================================================
// FILE: ReactivatePlatformTenantResponse.cs
//
// PURPOSE:
// Result of ReactivatePlatformTenant.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReactivatePlatformTenantResponse
{
    private ReactivatePlatformTenantResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReactivatePlatformTenantResponse Success()
    {
        return new ReactivatePlatformTenantResponse { Succeeded = true };
    }

    public static ReactivatePlatformTenantResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReactivatePlatformTenantResponse { Error = error };
    }
}
