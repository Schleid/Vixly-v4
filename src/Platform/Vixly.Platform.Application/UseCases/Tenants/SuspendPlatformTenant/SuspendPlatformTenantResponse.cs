using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.SuspendPlatformTenant;

// =========================================================
// FILE: SuspendPlatformTenantResponse.cs
//
// PURPOSE:
// Result of SuspendPlatformTenant.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SuspendPlatformTenantResponse
{
    private SuspendPlatformTenantResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SuspendPlatformTenantResponse Success()
    {
        return new SuspendPlatformTenantResponse { Succeeded = true };
    }

    public static SuspendPlatformTenantResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SuspendPlatformTenantResponse { Error = error };
    }
}
