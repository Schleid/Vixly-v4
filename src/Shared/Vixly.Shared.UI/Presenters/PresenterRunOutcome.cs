namespace Vixly.Shared.UI.Presenters;

// =========================================================
// FILE: PresenterRunOutcome.cs
//
// PURPOSE:
// How one presenter operation ended.
//
// RESPONSIBILITIES:
// - Name the four ways RunAsync can end
//
// IMPORTANT NOTES:
// - Completed: the work ran; the plane presenter reads its own
//   result for business failures
// - Denied: an authorization gate refused (S-1)
// - Failed: an unexpected exception, logged with a reference
//   the user can quote (S-9)
// - Cancelled: the component went away; show nothing
// =========================================================
public enum PresenterRunOutcome
{
    Completed = 0,
    Denied = 1,
    Failed = 2,
    Cancelled = 3
}
