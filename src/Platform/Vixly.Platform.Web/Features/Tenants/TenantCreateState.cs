using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantCreateState.cs
//
// PURPOSE:
// State of the new-tenant form.
//
// RESPONSIBILITIES:
// - Hold the outcome of the last attempt
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantCreateState(Guid? CreatedTenantId, string BaseDomain, Error? Error)
{
    public static TenantCreateState Initial { get; } = new(null, string.Empty, null);
}
