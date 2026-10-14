using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformUserListItem.cs
//
// PURPOSE:
// One row of the staff user list.
//
// RESPONSIBILITIES:
// - Carry what the list shows, nothing more
//
// IMPORTANT NOTES:
// - A projection: never a tracked entity (P-1)
// =========================================================
public sealed record PlatformUserListItem(
    Guid PlatformUserId,
    string FirstName,
    string LastName,
    string Email,
    PlatformUserAccountStatus Status,
    PlatformUserAuthorityLevel AuthorityLevel,
    DateTimeOffset? LastSignInUtc);
