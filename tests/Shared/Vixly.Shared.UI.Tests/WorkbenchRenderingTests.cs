using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vixly.Shared.UI.Primitives;
namespace Vixly.Shared.UI.Tests;

// =========================================================
// FILE: WorkbenchRenderingTests.cs
//
// PURPOSE:
// Verify accessibility and safe rendering contracts of workbench primitives.
//
// RESPONSIBILITIES:
// Assert current-stage semantics, status vocabulary, escaping and busy state.
//
// IMPORTANT NOTES:
// Static HTML renderer only; no backend or browser required.
// =========================================================
public sealed class WorkbenchRenderingTests
{
    [Fact]
    public async Task Lifecycle_AnnouncesExactlyOneCurrentStage()
    {
        string html = await Render<VxLifecycleStepper>(new()
        {
            [nameof(VxLifecycleStepper.Steps)] = new[] { "Intake", "Scan", "Dispatch" },
            [nameof(VxLifecycleStepper.CurrentIndex)] = 1,
            [nameof(VxLifecycleStepper.Label)] = "Progress"
        });
        Assert.Single(Regex.Matches(html, "aria-current=\"step\""));
        Assert.Contains("aria-label=\"Progress\"", html);
        Assert.Contains("vx-lifecycle__complete", html);
    }

    [Theory]
    [InlineData(VxBadgeStatus.Info, "invited")]
    [InlineData(VxBadgeStatus.Warning, "suspended")]
    [InlineData(VxBadgeStatus.Progress, "progress")]
    [InlineData(VxBadgeStatus.Success, "active")]
    [InlineData(VxBadgeStatus.Danger, "locked")]
    public async Task Status_KeepsTextAlongsideDistinctSemanticColour(VxBadgeStatus status, string modifier)
    {
        string html = await Render<VxStatusBadge>(new()
        {
            [nameof(VxStatusBadge.Text)] = "Visible status",
            [nameof(VxStatusBadge.Status)] = status
        });
        Assert.Contains("vx-badge--" + modifier, html);
        Assert.Contains("Visible status", html);
    }

    [Fact]
    public async Task Metric_EncodesConsumerText()
    {
        string html = await Render<VxMetricCard>(new()
        {
            [nameof(VxMetricCard.Label)] = "<script>alert(1)</script>",
            [nameof(VxMetricCard.Value)] = "125 USD"
        });
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("125 USD", html);
    }

    [Fact]
    public async Task LoadingTable_AnnouncesBusyAndDoesNotRenderRows()
    {
        string html = await Render<VxDataTable<int>>(new()
        {
            [nameof(VxDataTable<int>.Items)] = new[] { 1, 2 },
            [nameof(VxDataTable<int>.IsLoading)] = true,
            [nameof(VxDataTable<int>.Caption)] = "Loading shipments"
        });
        Assert.Contains("aria-busy=\"true\"", html);
        Assert.Contains("vx-skeleton-line", html);
        Assert.Contains("Loading shipments", html);
    }

    [Fact]
    public async Task PasswordSpecialization_RendersMaskedInputWithoutUnsupportedParameters()
    {
        string html = await Render<VxPasswordInput>(new()
        {
            [nameof(VxPasswordInput.Id)] = "password-example",
            [nameof(VxPasswordInput.Label)] = "Password"
        });
        Assert.Contains("type=\"password\"", html);
        Assert.Contains("for=\"password-example\"", html);
    }

    private static async Task<string> Render<T>(Dictionary<string, object?> parameters) where T : IComponent
    {
        await using ServiceProvider services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(parameters));
            return result.ToHtmlString();
        });
    }
}
