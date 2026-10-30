﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Vixly.Shared.UI.Presenters;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: SlowSubmitForm.cs
//
// PURPOSE:
// A test-only guarded form whose submit waits until the test
// lets it finish (ADR-0019 tests).
//
// RESPONSIBILITIES:
// - Count submits, clicks and typed changes that reached it
//
// IMPORTANT NOTES:
// - Test fixture; never used by the application
// =========================================================
internal sealed class SlowSubmitForm : GuardedComponent
{
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int Submits { get; private set; }

    public int Changes { get; private set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "form");
        builder.AddAttribute(1, "onsubmit", EventCallback.Factory.Create(this, SubmitAsync));
        builder.OpenElement(2, "input");
        builder.AddAttribute(3, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, _ => Changes++));
        builder.CloseElement();
        builder.OpenElement(4, "button");
        builder.AddAttribute(5, "type", "submit");
        builder.AddAttribute(6, "disabled", IsActing);
        builder.CloseElement();
        builder.CloseElement();
    }

    private async Task SubmitAsync()
    {
        Submits++;
        await Release.Task;
    }
}
