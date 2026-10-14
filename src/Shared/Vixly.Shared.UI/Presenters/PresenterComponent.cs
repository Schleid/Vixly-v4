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
// - Let a plane prepare the scope before the presenter is
//   created (PrepareScope)
//
// IMPORTANT NOTES:
// - The presenter is resolved on first use of Presenter, after
//   PrepareScope; a plane base uses this to copy the signed-in
//   identity into the new scope
// - Owned by the Frontend/UI team (Shared.UI); review with them
// =========================================================
public abstract class PresenterComponent<TPresenter> : OwningComponentBase<TPresenter>
    where TPresenter : PresenterBase
{
    private bool _attached;

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

    protected virtual void PrepareScope(IServiceProvider scopedServices)
    {
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
