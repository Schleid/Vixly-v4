using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformUserDetail.cs
//
// PURPOSE:
// Everything the user detail screen shows about one staff user.
//
// RESPONSIBILITIES:
// - Carry profile, account state, second-factor summary and
//   open sessions
//
// IMPORTANT NOTES:
// - A projection: never a tracked entity (P-1)
// - Second factors are counts only; no secret or credential
//   data leaves Infrastructure
// - Roles and departments come from GetPlatformUserRoles and
//   GetPlatformUserDepartments
// =========================================================
public sealed record PlatformUserDetail
{
    public Guid PlatformUserId { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? EmployeeNumber { get; init; }

    public string? PhoneNumber { get; init; }

    public string PreferredLanguage { get; init; } = string.Empty;

    public PlatformUserAuthorityLevel AuthorityLevel { get; init; }

    public PlatformUserAccountStatus Status { get; init; }

    public string? StatusReason { get; init; }

    public DateTimeOffset? StatusChangedUtc { get; init; }

    public DateTimeOffset CreatedUtc { get; init; }

    public DateTimeOffset? LastSignInUtc { get; init; }

    public bool HasAuthenticatorApp { get; init; }

    public int PasskeyCount { get; init; }

    public IReadOnlyList<PlatformUserPasskeyItem> Passkeys { get; init; } = Array.Empty<PlatformUserPasskeyItem>();

    public int UnusedRecoveryCodeCount { get; init; }

    public IReadOnlyList<PlatformUserSessionItem> OpenSessions { get; init; } = Array.Empty<PlatformUserSessionItem>();
}
