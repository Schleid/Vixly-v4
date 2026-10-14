namespace Vixly.Platform.Web.Features;

// =========================================================
// FILE: PlatformUserWorkspace.cs
//
// PURPOSE:
// Tells the components of one screen that a staff user changed
// (UI-3).
//
// RESPONSIBILITIES:
// - Carry the UserChanged notification between sibling feature
//   components
//
// IMPORTANT NOTES:
// - Scoped to the circuit; components subscribe and refresh
//   themselves. It carries ids, never data
// =========================================================
public sealed class PlatformUserWorkspace
{
    public event Action<Guid>? UserChanged;

    public void NotifyUserChanged(Guid platformUserId)
    {
        UserChanged?.Invoke(platformUserId);
    }
}
