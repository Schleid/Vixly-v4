using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.Users.InvitePlatformUser;

// =========================================================
// FILE: InvitePlatformUserRequest.cs
//
// PURPOSE:
// Input for InvitePlatformUser.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The inviter comes from the context, never the request
// =========================================================
public sealed class InvitePlatformUserRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PreferredLanguage { get; init; } = "en";

    public PlatformUserAuthorityLevel AuthorityLevel { get; init; }

    public string? EmployeeNumber { get; init; }

    public string? PhoneNumber { get; init; }
}
