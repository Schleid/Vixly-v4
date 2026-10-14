using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Account;

// =========================================================
// FILE: MyPasswordState.cs
//
// PURPOSE:
// What the password card of My account shows.
//
// RESPONSIBILITIES:
// - Carry the error, whether the change worked and how many other
//   sessions it ended, and whether the session ended
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record MyPasswordState(Error? Error, bool Changed, int OtherSessionsEnded, bool SessionEnded)
{
    public static MyPasswordState Initial { get; } = new(null, false, 0, false);
}
