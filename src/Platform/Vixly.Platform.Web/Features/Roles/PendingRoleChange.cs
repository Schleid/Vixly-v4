namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: PendingRoleChange.cs
//
// PURPOSE:
// A role change waiting for step-up.
//
// RESPONSIBILITIES:
// - Remember whether it gives or removes a role, which one, and
//   until when
//
// IMPORTANT NOTES:
// - Resumed as-is once the second factor is confirmed
// =========================================================
public sealed record PendingRoleChange(bool IsAssign, Guid PlatformRoleId, DateTimeOffset? ExpiresUtc);
