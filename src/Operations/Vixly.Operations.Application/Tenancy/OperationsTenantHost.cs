namespace Vixly.Operations.Application.Tenancy;

// =========================================================
// FILE: OperationsTenantHost.cs
//
// PURPOSE:
// The resolved tenant of a host name.
//
// RESPONSIBILITIES:
// - Carry the state, tenant, access, address and language
//
// IMPORTANT NOTES:
// - Built only by ResolveOperationsTenant
// - Legal is Platform's Terms status: not_required, accepted, due, overdue (Slice 8)
// =========================================================
public sealed record OperationsTenantHost(OperationsTenantState State, Guid? TenantId, OperationsTenantAccess Access, string? CanonicalSlug, string? DefaultLanguage, string Legal = "not_required")
{
    public static OperationsTenantHost NotFound { get; } = new(OperationsTenantState.NotFound, null, OperationsTenantAccess.None, null, null);
}
