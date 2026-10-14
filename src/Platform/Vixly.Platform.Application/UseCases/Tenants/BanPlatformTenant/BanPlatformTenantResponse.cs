using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.BanPlatformTenant;

// =========================================================
// FILE: BanPlatformTenantResponse.cs
//
// PURPOSE:
// Result of BanPlatformTenant.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class BanPlatformTenantResponse
{
    private BanPlatformTenantResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static BanPlatformTenantResponse Success()
    {
        return new BanPlatformTenantResponse { Succeeded = true };
    }

    public static BanPlatformTenantResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BanPlatformTenantResponse { Error = error };
    }
}
