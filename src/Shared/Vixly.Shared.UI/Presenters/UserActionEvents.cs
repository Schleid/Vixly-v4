using Microsoft.AspNetCore.Components.Web;

namespace Vixly.Shared.UI.Presenters;

// =========================================================
// FILE: UserActionEvents.cs
//
// PURPOSE:
// Tells a user action (a click or a form submit) apart from other UI events (double-click protection, ADR-0019).
//
// RESPONSIBILITIES:
// - Say whether a UI event is a click, a double-click or a form submit
//
// IMPORTANT NOTES:
// - Typing, change and keyboard events are never actions: a scanner or a fast typist must never lose input
// - Blazor sends a form submit as a plain EventArgs
// =========================================================
public static class UserActionEvents
{
    public static bool IsUserAction(object? eventArgs)
    {
        return eventArgs switch
        {
            MouseEventArgs mouse => mouse.Type is "click" or "dblclick" or "" or null,
            null => false,
            _ => eventArgs.GetType() == typeof(EventArgs)
        };
    }
}
