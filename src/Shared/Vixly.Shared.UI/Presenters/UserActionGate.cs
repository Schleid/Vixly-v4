using Microsoft.AspNetCore.Components;

namespace Vixly.Shared.UI.Presenters;

// =========================================================
// FILE: UserActionGate.cs
//
// PURPOSE:
// Double-click protection for one component (ADR-0019).
//
// RESPONSIBILITIES:
// - Run a UI event handler the way ComponentBase does: render now, and again when it ends
// - Drop a click or submit while an earlier one of the same component is still running
//
// IMPORTANT NOTES:
// - Other events (typing, change, keys) always pass
// - One gate per component instance; the Blazor renderer calls it on one thread at a time
// =========================================================
public sealed class UserActionGate
{
    private int _inFlight;

    public bool IsActing => _inFlight > 0;

    public Task HandleAsync(EventCallbackWorkItem callback, object? arg, Action render)
    {
        ArgumentNullException.ThrowIfNull(render);
        bool action = UserActionEvents.IsUserAction(arg);

        if (action && _inFlight > 0)
        {
            return Task.CompletedTask;
        }

        if (action)
        {
            _inFlight++;
        }

        Task task;

        try
        {
            task = callback.InvokeAsync(arg);
        }
        catch
        {
            Leave(action);
            throw;
        }

        if (task.Status is TaskStatus.RanToCompletion or TaskStatus.Canceled)
        {
            Leave(action);
            render();
            return Task.CompletedTask;
        }

        render();
        return CompleteAsync(task, action, render);
    }

    private async Task CompleteAsync(Task task, bool action, Action render)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException) when (task.IsCanceled)
        {
            // Cancelled, as ComponentBase treats it: nothing more to render.
            Leave(action);
            return;
        }
        catch
        {
            Leave(action);
            throw;
        }

        Leave(action);
        render();
    }

    private void Leave(bool action)
    {
        if (action && _inFlight > 0)
        {
            _inFlight--;
        }
    }
}
