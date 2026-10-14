using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: UserInviteState.cs
//
// PURPOSE:
// What the invite form shows.
//
// RESPONSIBILITIES:
// - Carry whether higher authority levels can be chosen, the new
//   user id after success, and the error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record UserInviteState(bool Loaded, bool CanInvite, bool CanChooseAuthority, Guid? InvitedUserId, Error? Error)
{
    public static UserInviteState Initial { get; } = new(false, false, false, null, null);
}
