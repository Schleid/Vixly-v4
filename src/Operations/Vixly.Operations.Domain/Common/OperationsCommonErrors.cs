using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Common;

// =========================================================
// FILE: OperationsCommonErrors.cs
//
// PURPOSE:
// Error codes every Operations area uses (L-1).
//
// RESPONSIBILITIES:
// - Declare the shared codes once
//
// IMPORTANT NOTES:
// - Each code has a text in OperationsText (L-10)
// =========================================================
public static class OperationsCommonErrors
{
    public static Error TenantRequired { get; } = Error.Create("ops.common.tenant_required");

    public static Error NotFound { get; } = Error.Create("ops.common.not_found");

    public static Error ConcurrencyConflict { get; } = Error.Create("ops.common.concurrency_conflict");

    public static Error RequestInvalid { get; } = Error.Create("ops.common.request_invalid");

    public static Error PlatformUnavailable { get; } = Error.Create("ops.common.platform_unavailable");
}
