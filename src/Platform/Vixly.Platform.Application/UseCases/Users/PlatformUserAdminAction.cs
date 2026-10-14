namespace Vixly.Platform.Application.UseCases.Users;

// =========================================================
// FILE: PlatformUserAdminAction.cs
//
// PURPOSE:
// The administrator actions on a staff account that the
// console can offer.
//
// RESPONSIBILITIES:
// - Name each action GetPlatformUserActions can return
//
// IMPORTANT NOTES:
// - Each maps to one use case of the same name
// =========================================================
public enum PlatformUserAdminAction
{
    Lock = 0,
    Suspend = 1,
    Deactivate = 2,
    Reactivate = 3,
    ResetSecondFactor = 4,
    EndSessions = 5,
    ResendInvitation = 6,
    SendPasswordReset = 7
}
