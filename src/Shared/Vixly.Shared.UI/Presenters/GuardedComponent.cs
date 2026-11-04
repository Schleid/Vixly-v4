using Microsoft.AspNetCore.Components;

namespace Vixly.Shared.UI.Presenters;

// =========================================================
// FILE: GuardedComponent.cs
//
// PURPOSE:
// The base of an interactive component without a presenter, such as a form section that hands its data to its parent (ADR-0019).
//
// RESPONSIBILITIES:
// - Drop repeated clicks and submits while one is still running
//
// IMPORTANT NOTES:
// - Components with a presenter use PresenterComponent, which has the same guard
// =========================================================
public abstract class GuardedComponent : ComponentBase, IHandleEvent
{
    private readonly UserActionGate _gate = new();

    // True while a click or submit of this component is still running; markup may disable controls with it.
    protected bool IsActing => _gate.IsActing;

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        return _gate.HandleAsync(callback, arg, StateHasChanged);
    }
}
