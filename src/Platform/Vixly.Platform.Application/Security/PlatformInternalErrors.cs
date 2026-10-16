using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformInternalErrors.cs
//
// PURPOSE:
// Refusals of the internal channel before any use case runs (Slice 9, D-K1).
//
// RESPONSIBILITIES:
// - Name why a service token or caller was refused
//
// IMPORTANT NOTES:
// - Never say which check failed inside a token (signature, time, replay): one code for all
// =========================================================
public static class PlatformInternalErrors
{
    public static Error TokenInvalid { get; } =
        Error.Create("platform.internal.token_invalid");

    public static Error ScopeNotAllowed { get; } =
        Error.Create("platform.internal.scope_not_allowed");

    public static Error TenantRequired { get; } =
        Error.Create("platform.internal.tenant_required");

    public static Error TooManyItems { get; } =
        Error.Create("platform.internal.too_many_items", ("max", "100"));
}
