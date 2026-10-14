using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: SharedComponentsUiTests.cs
//
// PURPOSE:
// bUnit tests verifying rendering, states, accessibility
// and behavior for all shared UI primitives in Vixly.Shared.UI.
//
// RESPONSIBILITIES:
// - Verify button variants, busy states, disabled states
// - Verify card sections (header, actions, body, footer)
// - Verify status badge mappings and WCAG non-color-only rendering
// - Verify message roles (alert vs status) and dismissibility
// - Verify dialog modal attributes, focus trap hook, and close triggers
// - Verify pager page bounds, button disabling, and page changes
// - Verify data table loading skeleton, empty content, and row templates
// - Verify empty state structure
// - Verify text input label association and value binding
//
// IMPORTANT NOTES:
// - Tests pure presentational behavior without any platform dependencies
// =========================================================
public sealed class SharedComponentsUiTests : BunitContext
{
    [Fact]
    public void VxButton_RendersPrimaryVariant_AndTriggersClick()
    {
        bool clicked = false;
        var cut = Render<VxButton>(parameters => parameters
            .Add(p => p.Variant, VxButtonVariant.Primary)
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked = true))
            .AddChildContent("Save"));

        var button = cut.Find("button");
        Assert.Contains("vx-button--primary", button.ClassName);
        Assert.Equal("Save", button.TextContent.Trim());

        button.Click();
        Assert.True(clicked);
    }

    [Fact]
    public void VxButton_WhenBusy_RendersSpinnerAndIsDisabled()
    {
        var cut = Render<VxButton>(parameters => parameters
            .Add(p => p.IsBusy, true)
            .AddChildContent("Processing"));

        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
        Assert.Equal("true", button.GetAttribute("aria-busy"));
        Assert.NotNull(cut.Find(".vx-button__spinner"));
    }

    [Fact]
    public void VxButton_RendersDangerAndSubtleVariants()
    {
        var cutDanger = Render<VxButton>(parameters => parameters
            .Add(p => p.Variant, VxButtonVariant.Danger));
        Assert.Contains("vx-button--danger", cutDanger.Find("button").ClassName);

        var cutSubtle = Render<VxButton>(parameters => parameters
            .Add(p => p.Variant, VxButtonVariant.Subtle));
        Assert.Contains("vx-button--subtle", cutSubtle.Find("button").ClassName);
    }

    [Fact]
    public void VxCard_RendersTitleActionsBodyAndFooter()
    {
        var cut = Render<VxCard>(parameters => parameters
            .Add(p => p.Title, "Account Overview")
            .Add(p => p.HeaderActions, (RenderFragment)(builder => builder.AddContent(0, "Edit")))
            .Add(p => p.Footer, (RenderFragment)(builder => builder.AddContent(0, "Footer Note")))
            .AddChildContent("Body Content"));

        Assert.Equal("Account Overview", cut.Find(".vx-card__title").TextContent.Trim());
        Assert.Equal("Edit", cut.Find(".vx-card__actions").TextContent.Trim());
        Assert.Equal("Body Content", cut.Find(".vx-card__body").TextContent.Trim());
        Assert.Equal("Footer Note", cut.Find(".vx-card__footer").TextContent.Trim());
    }

    [Theory]
    [InlineData(VxBadgeStatus.Active, "vx-badge--active")]
    [InlineData(VxBadgeStatus.Invited, "vx-badge--invited")]
    [InlineData(VxBadgeStatus.Suspended, "vx-badge--suspended")]
    [InlineData(VxBadgeStatus.Locked, "vx-badge--locked")]
    [InlineData(VxBadgeStatus.Deactivated, "vx-badge--deactivated")]
    public void VxStatusBadge_RendersCorrectStatusModifier(VxBadgeStatus status, string expectedClass)
    {
        var cut = Render<VxStatusBadge>(parameters => parameters
            .Add(p => p.Status, status)
            .Add(p => p.Text, "Status Text"));

        var badge = cut.Find(".vx-badge");
        Assert.Contains(expectedClass, badge.ClassName);
        Assert.Contains("Status Text", cut.Find(".vx-badge__text").TextContent);
    }

    [Fact]
    public void VxMessage_RendersAlertRoleForError_AndStatusRoleForInfo()
    {
        var cutError = Render<VxMessage>(parameters => parameters
            .Add(p => p.Type, VxMessageType.Error)
            .Add(p => p.Text, "Something failed"));

        var errorEl = cutError.Find(".vx-message");
        Assert.Equal("alert", errorEl.GetAttribute("role"));
        Assert.Contains("vx-message--error", errorEl.ClassName);
        Assert.Contains("Something failed", errorEl.TextContent);

        var cutInfo = Render<VxMessage>(parameters => parameters
            .Add(p => p.Type, VxMessageType.Info)
            .Add(p => p.Text, "Notice"));

        var infoEl = cutInfo.Find(".vx-message");
        Assert.Equal("status", infoEl.GetAttribute("role"));
        Assert.Contains("vx-message--info", infoEl.ClassName);
    }

    [Fact]
    public void VxMessage_WhenDismissible_RendersDismissButtonAndTriggersCallback()
    {
        bool dismissed = false;
        var cut = Render<VxMessage>(parameters => parameters
            .Add(p => p.Dismissible, true)
            .Add(p => p.DismissAriaLabel, "Close notice")
            .Add(p => p.OnDismiss, EventCallback.Factory.Create(this, () => dismissed = true))
            .Add(p => p.Text, "Dismissible text"));

        var button = cut.Find("button.vx-message__dismiss");
        Assert.Equal("Close notice", button.GetAttribute("aria-label"));

        button.Click();
        Assert.True(dismissed);
    }

    [Fact]
    public void VxDialog_WhenClosed_RendersNothing()
    {
        var cut = Render<VxDialog>(parameters => parameters
            .Add(p => p.IsOpen, false)
            .Add(p => p.Title, "Step up"));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void VxDialog_WhenOpen_RendersModalBackdropAndClosesOnEscape()
    {
        bool closed = false;
        var cut = Render<VxDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Confirm Identity")
            .Add(p => p.CloseAriaLabel, "Close dialog")
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closed = true))
            .AddChildContent("Enter passkey"));

        var dialog = cut.Find(".vx-dialog");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Equal("Confirm Identity", cut.Find(".vx-dialog__title").TextContent.Trim());
        Assert.Equal("Close dialog", cut.Find(".vx-dialog__close").GetAttribute("aria-label"));

        // Press Escape key
        cut.Find(".vx-dialog-overlay").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(closed);
    }

    [Fact]
    public void VxPager_DisablesPreviousOnFirstPage_AndTriggersNextPage()
    {
        int targetPage = 0;
        var cut = Render<VxPager>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.PreviousAriaLabel, "Previous")
            .Add(p => p.NextAriaLabel, "Next")
            .Add(p => p.StatusText, "Page 1 of 5")
            .Add(p => p.PageChanged, EventCallback.Factory.Create<int>(this, page => targetPage = page)));

        var buttons = cut.FindAll("button");
        Assert.True(buttons[0].HasAttribute("disabled"));
        Assert.False(buttons[1].HasAttribute("disabled"));
        Assert.Equal("Page 1 of 5", cut.Find(".vx-pager__status").TextContent.Trim());

        buttons[1].Click();
        Assert.Equal(2, targetPage);
    }

    [Fact]
    public void VxPager_DisablesNextOnLastPage()
    {
        var cut = Render<VxPager>(parameters => parameters
            .Add(p => p.CurrentPage, 5)
            .Add(p => p.TotalPages, 5));

        var buttons = cut.FindAll("button");
        Assert.False(buttons[0].HasAttribute("disabled"));
        Assert.True(buttons[1].HasAttribute("disabled"));
    }

    [Fact]
    public void VxDataTable_RendersLoadingSkeleton_WhenIsLoadingTrue()
    {
        var cut = Render<VxDataTable<string>>(parameters => parameters
            .Add(p => p.IsLoading, true));

        Assert.NotNull(cut.Find(".vx-table__loading"));
        Assert.NotNull(cut.Find(".vx-skeleton-line"));
    }

    [Fact]
    public void VxDataTable_RendersEmptyContent_WhenItemsEmpty()
    {
        var cut = Render<VxDataTable<string>>(parameters => parameters
            .Add(p => p.Items, Array.Empty<string>())
            .Add(p => p.EmptyContent, (RenderFragment)(b => b.AddContent(0, "No records found"))));

        Assert.Contains("No records found", cut.Find(".vx-table__empty").TextContent);
    }

    [Fact]
    public void VxDataTable_RendersRows_WhenItemsProvided()
    {
        var items = new[] { "Alpha", "Beta" };
        var cut = Render<VxDataTable<string>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.HeaderContent, (RenderFragment)(b => b.AddMarkupContent(0, "<th>Name</th>")))
            .Add(p => p.RowTemplate, item => (RenderFragment)(b => b.AddMarkupContent(0, $"<td>{item}</td>"))));

        var cells = cut.FindAll("tbody tr td");
        Assert.Equal(2, cells.Count);
        Assert.Equal("Alpha", cells[0].TextContent);
        Assert.Equal("Beta", cells[1].TextContent);
    }

    [Fact]
    public void VxEmptyState_RendersTitleAndDescription()
    {
        var cut = Render<VxEmptyState>(parameters => parameters
            .Add(p => p.Title, "No users")
            .Add(p => p.Description, "Invite users to get started"));

        Assert.Equal("No users", cut.Find(".vx-empty-state__title").TextContent.Trim());
        Assert.Equal("Invite users to get started", cut.Find(".vx-empty-state__desc").TextContent.Trim());
    }

    [Fact]
    public void VxTextInput_WithLabel_RendersLabelAndInputConnected()
    {
        string? inputVal = null;
        var cut = Render<VxTextInput>(parameters => parameters
            .Add(p => p.Id, "user-email")
            .Add(p => p.Label, "Email Address")
            .Add(p => p.Value, "admin@example.com")
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, val => inputVal = val)));

        var label = cut.Find("label.vx-label");
        Assert.Equal("user-email", label.GetAttribute("for"));
        Assert.Equal("Email Address", label.TextContent.Trim());

        var input = cut.Find("input.vx-input");
        Assert.Equal("user-email", input.Id);
        Assert.Equal("admin@example.com", input.GetAttribute("value"));

        input.Input("new@example.com");
        Assert.Equal("new@example.com", inputVal);
    }
}
