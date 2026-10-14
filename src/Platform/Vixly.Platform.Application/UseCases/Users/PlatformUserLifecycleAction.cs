using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.Users;

// =========================================================
// FILE: PlatformUserLifecycleAction.cs
//
// PURPOSE:
// Describes one administrator action for
// PlatformUserLifecycleWorkflow.
//
// RESPONSIBILITIES:
// - Name the target, permission, audit code and the domain
//   call to make
// - Say whether the target's sessions end, and why
//
// IMPORTANT NOTES:
// - Apply receives (user, actor id, other active SuperUsers,
//   now)
// =========================================================
public sealed record PlatformUserLifecycleAction(
    Guid TargetUserId,
    string PermissionCode,
    string AuditActionCode,
    Action<PlatformUser, Guid, int, DateTimeOffset> Apply,
    PlatformSessionEndReason? EndSessionsWith);
