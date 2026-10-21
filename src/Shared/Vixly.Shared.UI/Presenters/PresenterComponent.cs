using Microsoft.AspNetCore.Components;

namespace Vixly.Shared.UI.Presenters;

// =========================================================
// FILE: PresenterComponent.cs
//
// PURPOSE:
// The base of every feature component that has a presenter
// (ADR-0001, UI-4).
//
// RESPONSIBILITIES:
// - Own a DI scope that lives and dies with the component, so
//   the presenter and its use cases are disposed with it
// - Re-render when the presenter raises StateChanged
// - Drop repeated clicks and submits while one is still
//   running (double-click protection, ADR-0019)
// - Let a plane prepare the scope before the presenter is
//   created (PrepareScope)
//
// IMPORTANT NOTES:
// - The presenter is resolved on first use of Presenter, after
//   PrepareScope; a plane base uses this to copy the signed-in
//   identity into the new scope
// - Owned by the Frontend/UI team (Shared.UI); review with them
// =========================================================
public abstract class PresenterComponent<TPresenter> : OwningComponentBase<TPresenter>, IHandleEvent
    where TPresenter : PresenterBase
{
    private bool _attached;

    private readonly UserActionGate _gate = new();

    protected TPresenter Presenter
    {
        get
        {
            if (!_attached)
            {
                PrepareScope(ScopedServices);
                Service.StateChanged += OnPresenterStateChanged;
                _attached = true;
            }

            return Service;
        }
    }

    // True while a click or submit of this component is still running; markup may disable controls with it.
    protected bool IsActing => _gate.IsActing;

    protected virtual void PrepareScope(IServiceProvider scopedServices)
    {
    }

    // Double-click protection (ADR-0019): while one click or submit is still running,
    // further clicks and submits on this component are dropped. Other events pass.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        return _gate.HandleAsync(callback, arg, StateHasChanged);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _attached)
        {
            Service.StateChanged -= OnPresenterStateChanged;
        }

        base.Dispose(disposing);
    }

    private void OnPresenterStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }
}
