using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants;

// =========================================================
// FILE: PlatformTenantTarget.cs
//
// PURPOSE:
// The tenant a use case acts on, or why it may not.
//
// RESPONSIBILITIES:
// - Carry the loaded tenant or the refusal
//
// IMPORTANT NOTES:
// - Built only by PlatformTenantAdministration
// =========================================================
public sealed class PlatformTenantTarget
{
    private PlatformTenantTarget(PlatformTenant? tenant, Error? error)
    {
        Tenant = tenant;
        Error = error;
    }

    public PlatformTenant? Tenant { get; }

    public Error? Error { get; }

    public static PlatformTenantTarget Found(PlatformTenant tenant)
    {
        return new PlatformTenantTarget(tenant, null);
    }

    public static PlatformTenantTarget Refused(Error error)
    {
        return new PlatformTenantTarget(null, error);
    }
}
