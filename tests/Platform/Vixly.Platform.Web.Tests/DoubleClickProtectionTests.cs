﻿using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Vixly.Shared.UI.Presenters;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: DoubleClickProtectionTests.cs
//
// PURPOSE:
// Proves double-click protection (ADR-0019) on the server side
// of live console pages.
//
// RESPONSIBILITIES:
// - VxButton runs its handler once while it is still running
// - A guarded component drops repeated submits but never typing
// - The guard opens again once the first action ends
//
// IMPORTANT NOTES:
// - Handlers wait on a TaskCompletionSource, standing in for a
//   slow use case
// =========================================================
public sealed class DoubleClickProtectionTests : BunitContext
{
    [Fact]
    public async Task VxButton_IgnoresClicks_WhileItsHandlerRuns()
    {
        int calls = 0;
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IRenderedComponent<VxButton> cut = Render<VxButton>(parameters => parameters
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, async () =>
            {
                calls++;
                await release.Task;
            }))
            .AddChildContent("Pay"));

        _ = cut.Find("button").ClickAsync(new MouseEventArgs { Type = "click" });
        Assert.True(cut.Find("button").HasAttribute("disabled"));
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-busy"));

        cut.Find("button").RemoveAttribute("disabled");
        _ = cut.Find("button").ClickAsync(new MouseEventArgs { Type = "click" });
        _ = cut.Find("button").ClickAsync(new MouseEventArgs { Type = "click" });
        Assert.Equal(1, calls);

        release.SetResult();
        cut.WaitForAssertion(() => Assert.False(cut.Find("button").HasAttribute("disabled")));
        await cut.Find("button").ClickAsync(new MouseEventArgs { Type = "click" });
        Assert.Equal(2, calls);
    }

    [Fact]
    public void GuardedComponent_DropsRepeatedSubmits_ButKeepsTyping()
    {
        IRenderedComponent<SlowSubmitForm> cut = Render<SlowSubmitForm>();

        _ = cut.Find("form").SubmitAsync();
        Assert.True(cut.Find("button").HasAttribute("disabled"));
        _ = cut.Find("form").SubmitAsync();
        _ = cut.Find("form").SubmitAsync();
        cut.Find("input").Change("ABC123");

        Assert.Equal(1, cut.Instance.Submits);
        Assert.Equal(1, cut.Instance.Changes);

        cut.Instance.Release.SetResult();
        cut.WaitForAssertion(() => Assert.False(cut.Find("button").HasAttribute("disabled")));
        _ = cut.Find("form").SubmitAsync();
        Assert.Equal(2, cut.Instance.Submits);
    }

    [Theory]
    [InlineData("click", true)]
    [InlineData("dblclick", true)]
    [InlineData("mouseover", false)]
    public void OnlyClicksAndSubmits_CountAsActions(string type, bool action)
    {
        Assert.Equal(action, UserActionEvents.IsUserAction(new MouseEventArgs { Type = type }));
        Assert.True(UserActionEvents.IsUserAction(EventArgs.Empty));
        Assert.False(UserActionEvents.IsUserAction(new ChangeEventArgs()));
        Assert.False(UserActionEvents.IsUserAction(new KeyboardEventArgs()));
    }
}
