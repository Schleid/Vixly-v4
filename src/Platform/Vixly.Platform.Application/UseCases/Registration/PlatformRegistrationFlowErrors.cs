using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registration;

// =========================================================
// FILE: PlatformRegistrationFlowErrors.cs
//
// PURPOSE:
// Errors raised by the registration use cases rather than the
// registration model.
//
// RESPONSIBILITIES:
// - Declare the codes the use cases add
//
// IMPORTANT NOTES:
// - Codes start with platform.registrations. (Global Standard 4.2)
// - Each code needs en, fr and es text (L-10)
// =========================================================
public static class PlatformRegistrationFlowErrors
{
    public static Error BotCheckFailed { get; } =
        Error.Create("platform.registrations.bot_check_failed");

    public static Error InvitationRequired { get; } =
        Error.Create("platform.registrations.invitation_required");
}
