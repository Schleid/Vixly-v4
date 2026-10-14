using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Messaging;

// =========================================================
// FILE: PlatformMessagingErrors.cs
//
// PURPOSE:
// The errors raised by the outbox model.
//
// RESPONSIBILITIES:
// - Declare every outbox error code in one place
//
// IMPORTANT NOTES:
// - Codes start with platform.messaging. (Global Standard 4.2)
// - Each code needs en, fr and es text (L-10)
// =========================================================
public static class PlatformMessagingErrors
{
    public static Error TypeInvalid { get; } =
        Error.Create("platform.messaging.type_invalid");

    public static Error PayloadRequired { get; } =
        Error.Create("platform.messaging.payload_required");

    public static Error StreamRequired { get; } =
        Error.Create("platform.messaging.stream_required");
}
