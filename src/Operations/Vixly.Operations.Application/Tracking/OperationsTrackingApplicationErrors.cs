using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Tracking;

// =========================================================
// FILE: OperationsTrackingApplicationErrors.cs
//
// PURPOSE:
// Refusals of the tracking service (Slice 3A).
//
// RESPONSIBILITIES:
// - Name the refusals that need stored events to decide
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsTrackingApplicationErrors
{
    // The key belongs to an event about another subject or with another code.
    public static Error IdempotencyKeyReused { get; } = Error.Create("ops.tracking.idempotency_key_reused");

    // The event to void is unknown, about another subject, already voided, or itself a void.
    public static Error VoidTargetInvalid { get; } = Error.Create("ops.tracking.void_target_invalid");
}
