using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Messaging;

// =========================================================
// FILE: OperationsMessagingErrors.cs
//
// PURPOSE:
// Error codes of the Operations outbox.
//
// RESPONSIBILITIES:
// - Declare the outbox codes
//
// IMPORTANT NOTES:
// - Texts in OperationsText (L-10)
// =========================================================
public static class OperationsMessagingErrors
{
    public static Error TypeInvalid { get; } = Error.Create("ops.messaging.type_invalid");

    public static Error StreamRequired { get; } = Error.Create("ops.messaging.stream_required");

    public static Error PayloadRequired { get; } = Error.Create("ops.messaging.payload_required");
}
