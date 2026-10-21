using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantChangeErrors.cs
//
// PURPOSE:
// Refusals of company change requests (B12).
//
// RESPONSIBILITIES:
// - Name each refusal with a stable code
//
// IMPORTANT NOTES:
// - Codes follow the plane-first style (Global Standard 4.2)
// =========================================================
public static class PlatformTenantChangeErrors
{
    public static Error ReasonRequired { get; } = Error.Create("platform.tenant_changes.reason_required");

    public static Error ReasonTooLong { get; } = Error.Create("platform.tenant_changes.reason_too_long");

    public static Error NoDifference { get; } = Error.Create("platform.tenant_changes.no_difference");

    public static Error ProofRequired { get; } = Error.Create("platform.tenant_changes.proof_required");

    public static Error ProofTypeInvalid { get; } = Error.Create("platform.tenant_changes.proof_type_invalid");

    public static Error ProofTooLarge { get; } = Error.Create("platform.tenant_changes.proof_too_large");

    public static Error NotPending { get; } = Error.Create("platform.tenant_changes.not_pending");

    public static Error NoteRequired { get; } = Error.Create("platform.tenant_changes.note_required");

    public static Error NoteTooLong { get; } = Error.Create("platform.tenant_changes.note_too_long");

    public static Error AlreadyPending { get; } = Error.Create("platform.tenant_changes.already_pending");

    public static Error NotFound { get; } = Error.Create("platform.tenant_changes.not_found");
}
