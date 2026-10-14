namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformRoleListItem.cs
//
// PURPOSE:
// One row of the roles list.
//
// RESPONSIBILITIES:
// - Carry the name, system key, state, permission codes and the
//   number of people who hold the role now
//
// IMPORTANT NOTES:
// - A system role shows its translated name; SystemKey picks it
// =========================================================
public sealed record PlatformRoleListItem(
    Guid PlatformRoleId,
    string Name,
    string? SystemKey,
    bool IsActive,
    IReadOnlyList<string> PermissionCodes,
    int HolderCount);
