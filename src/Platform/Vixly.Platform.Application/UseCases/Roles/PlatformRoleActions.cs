namespace Vixly.Platform.Application.UseCases.Roles;

// =========================================================
// FILE: PlatformRoleActions.cs
//
// PURPOSE:
// What the signed-in user may do to one role.
//
// RESPONSIBILITIES:
// - Tell the role page which controls to offer
//
// IMPORTANT NOTES:
// - A convenience for the UI: every use case checks again
// =========================================================
public sealed record PlatformRoleActions(
    bool CanRename,
    bool CanDeactivate,
    bool CanReactivate,
    bool CanEditPermissions)
{
    public static PlatformRoleActions None { get; } = new(false, false, false, false);
}
