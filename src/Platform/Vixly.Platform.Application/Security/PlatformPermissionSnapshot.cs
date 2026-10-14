using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformPermissionSnapshot.cs
//
// PURPOSE:
// What persistence says about a user's access right now.
//
// RESPONSIBILITIES:
// - Carry the account state, authority level and the
//   permission codes of the user's active roles in force
//
// IMPORTANT NOTES:
// - Read fresh for every authorization check, so a role
//   revoked or an account locked a second ago already counts
// =========================================================
public sealed record PlatformPermissionSnapshot(
    bool IsAccountActive,
    PlatformUserAuthorityLevel AuthorityLevel,
    IReadOnlySet<string> PermissionCodes);
