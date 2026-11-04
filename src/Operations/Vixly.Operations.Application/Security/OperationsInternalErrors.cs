using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Security;

// =========================================================
// FILE: OperationsInternalErrors.cs
//
// PURPOSE:
// Error codes of the Operations internal channel (Stage 3, Slice 0, D-F4).
//
// RESPONSIBILITIES:
// - Declare the caller-token refusals
//
// IMPORTANT NOTES:
// - One code for every broken token, so a caller learns nothing about why
// =========================================================
public static class OperationsInternalErrors
{
    public static Error TokenInvalid { get; } = Error.Create("ops.internal.token_invalid");

    public static Error ScopeNotAllowed { get; } = Error.Create("ops.internal.scope_not_allowed");

    public static Error TenantRequired { get; } = Error.Create("ops.internal.tenant_required");
}
