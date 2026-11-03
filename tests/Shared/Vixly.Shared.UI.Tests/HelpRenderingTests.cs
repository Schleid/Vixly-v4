using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Shared.UI.Tests;

// =========================================================
// FILE: HelpRenderingTests.cs
//
// PURPOSE:
// Verifies the shared help foundation.
//
// RESPONSIBILITIES:
// - Enforce the help contract of ADR-0023.
//
// IMPORTANT NOTES:
// - No live application or customer data is required.
// =========================================================
public sealed class HelpRenderingTests
{
    [Fact]
    public async Task LinkHasAccessibleNameAndCallerAddress()
    {
        string html = await Render<VxHelpLink>(new()
        {
            [nameof(VxHelpLink.Href)] = "/en/help/welcome",
            [nameof(VxHelpLink.Label)] = "Open help"
        });
        Assert.Contains("href=\"/en/help/welcome\"", html);
        Assert.Contains("aria-label=\"Open help\"", html);
        Assert.Contains("aria-hidden=\"true\"", html);
    }

    [Fact]
    public async Task ArticleRendersTrustedHtmlButEncodesTitlesAndRelatedText()
    {
        string html = await Render<VxHelpArticle>(new()
        {
            [nameof(VxHelpArticle.Title)] = "<b>Title</b>",
            [nameof(VxHelpArticle.Html)] = "<h2 id=\"steps\">Steps</h2><p>Trusted article.</p>",
            [nameof(VxHelpArticle.RelatedLabel)] = "Related articles",
            [nameof(VxHelpArticle.Related)] = new[] { new VxHelpRelatedLink("<b>Related</b>", "/help/related") }
        });
        Assert.Contains("&lt;b&gt;Title&lt;/b&gt;", html);
        Assert.Contains("<h2 id=\"steps\">", html);
        Assert.Contains("&lt;b&gt;Related&lt;/b&gt;", html);
        Assert.Contains("aria-label=\"Related articles\"", html);
    }

    [Fact]
    public async Task EmptyRelatedListHasNoEmptyNavigation()
    {
        string html = await Render<VxHelpArticle>(new()
        {
            [nameof(VxHelpArticle.Title)] = "Welcome",
            [nameof(VxHelpArticle.Html)] = "<p>Welcome.</p>",
            [nameof(VxHelpArticle.RelatedLabel)] = "Related"
        });
        Assert.DoesNotContain("<nav", html);
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
