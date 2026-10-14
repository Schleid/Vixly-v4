namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformRoleHolderItem.cs
//
// PURPOSE:
// A staff user who holds a role now.
//
// RESPONSIBILITIES:
// - Carry the person, when the grant started and when it ends
//
// IMPORTANT NOTES:
// - ExpiresUtc is null for a grant without an end
// =========================================================
public sealed record PlatformRoleHolderItem(
    Guid PlatformUserId,
    string FirstName,
    string LastName,
    string Email,
    DateTimeOffset AssignedUtc,
    DateTimeOffset? ExpiresUtc);
