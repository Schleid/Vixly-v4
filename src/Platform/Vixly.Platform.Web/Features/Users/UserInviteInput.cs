using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: UserInviteInput.cs
//
// PURPOSE:
// The fields of the invite form while the administrator types.
//
// RESPONSIBILITIES:
// - Hold the form values the markup binds to
//
// IMPORTANT NOTES:
// - Mutable on purpose: it is form input, not state. The
//   presenter turns it into a request; the use case validates it
// =========================================================
public sealed class UserInviteInput
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PreferredLanguage { get; set; } = "en";

    public PlatformUserAuthorityLevel AuthorityLevel { get; set; } = PlatformUserAuthorityLevel.Standard;

    public string? EmployeeNumber { get; set; }

    public string? PhoneNumber { get; set; }
}
