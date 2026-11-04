using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Web.Components.Help;
using Vixly.Operations.Web.Features.Help;
using Vixly.Shared.Help;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsHelpRenderingTests.cs
//
// PURPOSE:
// Renders the actual help components with localized setup content.
//
// RESPONSIBILITIES:
// - Check readable branding, localized chrome and filtered topic links.
//
// IMPORTANT NOTES:
// - Component rendering does not certify database workflows or layout.
// =========================================================
public sealed class OperationsHelpRenderingTests
{
    [Theory]
    [InlineData("en")][InlineData("fr")][InlineData("es")]
    public async Task AllSetupArticlesRenderInTheirLanguage(string language)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            OperationsHelpCatalogue help = OperationsHelpTestCatalogue.Create();
            OperationsHelpTestReader source = new() { Context = new(new(OperationsPermissions.All, []), "Example Freight", "https://example.test/customer") };
            using OperationsHelpPresenter presenter = new(help, source, NullLogger<OperationsHelpPresenter>.Instance);
            foreach (HelpArticle article in help.Catalogue.Visible(language, source.Context.Reader))
            {
                await presenter.LoadAsync(language, article.Metadata.Slug, null);
                string html = await Render<OperationsHelpBrowser>(help, source, new() { [nameof(OperationsHelpBrowser.State)] = presenter.State });
                Assert.Contains(article.Metadata.Title, WebUtility.HtmlDecode(html));
                Assert.Contains("Example Freight", html);
                Assert.DoesNotContain("ui.help.", html);
                Assert.DoesNotContain("{brandName}", html);
                Assert.Contains("class=\"vx-help-article\"", html);
            }
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Fact]
    public async Task TopicLinksAreHiddenForDeniedMissingAndUnavailableArticles()
    {
        OperationsHelpCatalogue help = OperationsHelpTestCatalogue.Create();
        OperationsHelpTestReader source = new();
        Dictionary<string, object?> parameters = new() { [nameof(OperationsHelpArticleLink.Slug)] = "customers-mailbox" };
        Assert.DoesNotContain("<a", await Render<OperationsHelpArticleLink>(help, source, parameters));
        source.Context = source.Context with { Reader = new(["ops.warehouses.manage"], []) };
        string visible = await Render<OperationsHelpArticleLink>(help, source, parameters);
        Assert.Contains("href=\"/staff/help/customers-mailbox\"", visible);
        Assert.Contains("aria-label=", visible);
        parameters[nameof(OperationsHelpArticleLink.Slug)] = "unwritten-guide";
        Assert.DoesNotContain("<a", await Render<OperationsHelpArticleLink>(help, source, parameters));
        parameters[nameof(OperationsHelpArticleLink.Slug)] = "customers-mailbox";
        source.Context = source.Context with { BrandName = string.Empty };
        Assert.DoesNotContain("<a", await Render<OperationsHelpArticleLink>(help, source, parameters));
    }

    private static async Task<string> Render<T>(OperationsHelpCatalogue help, OperationsHelpTestReader source,
        Dictionary<string, object?> parameters) where T : IComponent
    {
        ServiceCollection collection = new();
        collection.AddLogging().AddLocalization();
        collection.AddSingleton(help).AddSingleton<IOperationsHelpReaderSource>(source);
        await using ServiceProvider services = collection.BuildServiceProvider();
        await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            // The link reads the language from the UI culture; fix it so any machine's locale gives the same page.
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");
            var result = await renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(parameters));
            return result.ToHtmlString();
        });
    }
}
