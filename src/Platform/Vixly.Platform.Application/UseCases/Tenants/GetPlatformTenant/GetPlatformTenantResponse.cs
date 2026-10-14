using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenant;

// =========================================================
// FILE: GetPlatformTenantResponse.cs
//
// PURPOSE:
// Result of GetPlatformTenant.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformTenantResponse
{
    private GetPlatformTenantResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantDetail? Tenant { get; private init; }

    public PlatformTenantAbilities Abilities { get; private init; } = PlatformTenantAbilities.None;

    public static GetPlatformTenantResponse Success(PlatformTenantDetail tenant, PlatformTenantAbilities abilities)
    {
        return new GetPlatformTenantResponse { Succeeded = true, Tenant = tenant, Abilities = abilities };
    }

    public static GetPlatformTenantResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformTenantResponse { Error = error };
    }
}
