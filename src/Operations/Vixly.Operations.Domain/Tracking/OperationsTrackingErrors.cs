using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsTrackingErrors.cs
//
// PURPOSE:
// Error codes of tracking events (Slice 3A).
//
// RESPONSIBILITIES:
// - Name every refusal of an event
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsTrackingErrors
{
    public static Error CodeUnknown { get; } = Error.Create("ops.tracking.code_unknown");

    public static Error SubjectRequired { get; } = Error.Create("ops.tracking.subject_required");

    public static Error ArgumentMissing { get; } = Error.Create("ops.tracking.argument_missing");

    public static Error ArgumentUnknown { get; } = Error.Create("ops.tracking.argument_unknown");

    public static Error ArgumentInvalid { get; } = Error.Create("ops.tracking.argument_invalid");

    public static Error IdempotencyKeyInvalid { get; } = Error.Create("ops.tracking.idempotency_key_invalid");

    public static Error VoidTargetRequired { get; } = Error.Create("ops.tracking.void_target_required");

    public static Error TimeInvalid { get; } = Error.Create("ops.tracking.time_invalid");

    public static Error LocationInvalid { get; } = Error.Create("ops.tracking.location_invalid");

    public static Error ActorInvalid { get; } = Error.Create("ops.tracking.actor_invalid");
}
