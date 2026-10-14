namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformUserRoleItem.cs
//
// PURPOSE:
// One role a staff user holds now.
//
// RESPONSIBILITIES:
// - Carry the role id, name and state, when the grant started and
//   when it ends
//
// IMPORTANT NOTES:
// - A system role shows its translated name; SystemKey picks it
// - A grant of an inactive role gives nothing until the role is
//   reactivated; it is listed so it can be revoked
// =========================================================
public sealed record PlatformUserRoleItem(
    Guid PlatformRoleId,
    string Name,
    string? SystemKey,
    bool RoleIsActive,
    DateTimeOffset AssignedUtc,
    DateTimeOffset? ExpiresUtc);
