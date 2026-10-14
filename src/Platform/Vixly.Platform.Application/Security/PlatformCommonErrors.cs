using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformCommonErrors.cs
//
// PURPOSE:
// Errors any Platform use case can return.
//
// RESPONSIBILITIES:
// - Not found, concurrency conflict, step-up required,
//   invalid request
//
// IMPORTANT NOTES:
// - NotFound is also returned when the caller may not know
//   whether a record exists (S-3)
// - StepUpRequired tells the console to ask for a second
//   factor and retry (ADR-0006 section 5)
// =========================================================
public static class PlatformCommonErrors
{
    public static Error NotFound { get; } =
        Error.Create("platform.common.not_found");

    public static Error ConcurrencyConflict { get; } =
        Error.Create("platform.common.concurrency_conflict");

    public static Error StepUpRequired { get; } =
        Error.Create("platform.common.step_up_required");

    public static Error RequestInvalid { get; } =
        Error.Create("platform.common.request_invalid");

    public static Error EmailAlreadyUsed { get; } =
        Error.Create("platform.common.email_already_used");

    public static Error PermissionUnknown { get; } =
        Error.Create("platform.common.permission_unknown");
}
