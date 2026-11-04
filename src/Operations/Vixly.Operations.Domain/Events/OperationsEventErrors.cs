using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Events;

// =========================================================
// FILE: OperationsEventErrors.cs
//
// PURPOSE:
// Error codes of operational events (Slice 6C).
//
// RESPONSIBILITIES:
// - Name every refusal
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsEventErrors
{
    public static Error NotFound { get; } = Error.Create("ops.events.not_found");

    public static Error KindInvalid { get; } = Error.Create("ops.events.kind_invalid");

    public static Error ReasonRequired { get; } = Error.Create("ops.events.reason_required");

    public static Error NoteRequired { get; } = Error.Create("ops.events.note_required");

    public static Error TextInvalid { get; } = Error.Create("ops.events.text_invalid");

    public static Error TimeInvalid { get; } = Error.Create("ops.events.time_invalid");

    public static Error NotManual { get; } = Error.Create("ops.events.not_manual");

    public static Error VoidReasonRequired { get; } = Error.Create("ops.events.void_reason_required");

    public static Error TooManyReasons { get; } = Error.Create("ops.events.too_many_reasons");
}
