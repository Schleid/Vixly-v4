using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformPermissionHolderItem.cs
//
// PURPOSE:
// A staff user who holds a permission now, and through what.
//
// RESPONSIBILITIES:
// - Carry the person and the role that gives the permission, or
//   no role when it comes from the SuperUser authority level
//
// IMPORTANT NOTES:
// - Only active accounts are listed: others hold nothing
// =========================================================
public sealed record PlatformPermissionHolderItem(
    Guid PlatformUserId,
    string FirstName,
    string LastName,
    string Email,
    PlatformUserAuthorityLevel AuthorityLevel,
    string? RoleName,
    string? RoleSystemKey,
    DateTimeOffset? ExpiresUtc,
    string? Language = null);
