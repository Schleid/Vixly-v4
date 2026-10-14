using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformSecurityErrors.cs
//
// PURPOSE:
// The catalogue of errors raised by Platform authorization.
//
// RESPONSIBILITIES:
// - Declare every Platform security error code in one place
//
// IMPORTANT NOTES:
// - Codes start with platform. (Global Standard 4.2)
// - Each code needs en, fr and es text in
//   Vixly.Platform.Resources; the architecture tests enforce
//   this once that project exists
// - AccessDenied is the code hosts return with a 403. It
//   deliberately does not say which permission was missing
//   (Global Standard S-9)
// =========================================================
public static class PlatformSecurityErrors
{
    public static Error UserIdRequired { get; } =
        Error.Create("platform.security.user_id_required");

    public static Error AuthorityLevelInvalid { get; } =
        Error.Create("platform.security.authority_level_invalid");

    public static Error AccessDenied { get; } =
        Error.Create("platform.security.access_denied");
}
