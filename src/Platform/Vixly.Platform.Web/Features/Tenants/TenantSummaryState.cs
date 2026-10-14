using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantSummaryState.cs
//
// PURPOSE:
// State of the tenant page header and overview.
//
// RESPONSIBILITIES:
// - Hold the tenant, what the actor may do and a load counter
//
// IMPORTANT NOTES:
// - Version changes on every reload so child sections refresh
// =========================================================
public sealed record TenantSummaryState(
    PlatformTenantDetail? Tenant,
    PlatformTenantAbilities Abilities,
    string BaseDomain,
    int Version,
    Error? Error)
{
    public static TenantSummaryState Initial { get; } = new(null, PlatformTenantAbilities.None, string.Empty, 0, null);
}
