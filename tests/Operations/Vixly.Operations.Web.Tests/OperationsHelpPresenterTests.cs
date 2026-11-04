using Microsoft.Extensions.Logging.Abstractions;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Web.Features.Help;
using Vixly.Shared.Help;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsHelpPresenterTests.cs
// PURPOSE:
// Verifies permission filtering and request-local company tokens.
// RESPONSIBILITIES:
// - Cover direct links, search, related links and contextual help.
// IMPORTANT NOTES:
// - Uses the published embedded articles and no database.
// =========================================================
public sealed class OperationsHelpPresenterTests
{
    private static OperationsHelpCatalogue Catalogue() => OperationsHelpTestCatalogue.Create();

    [Theory]
    [InlineData("en")][InlineData("fr")][InlineData("es")]
    public async Task SetupHelpIsHiddenAcrossEveryDiscoveryPath(string language)
    {
        OperationsHelpCatalogue help = Catalogue();
        OperationsHelpTestReader reader = new();
        using OperationsHelpPresenter presenter = new(help, reader, NullLogger<OperationsHelpPresenter>.Instance);
        await presenter.LoadAsync(language, null, null);
        Assert.Equal(5, presenter.State.Articles.Count);
        await presenter.LoadAsync(language, "setup-checklist", null);
        Assert.True(presenter.State.NotFound);
        Assert.Empty(presenter.State.Html);
        await presenter.LoadAsync(language, "workspace-at-a-glance", null);
        Assert.DoesNotContain(presenter.State.Related, article => article.Metadata.Slug == "setup-checklist");
        await presenter.LoadAsync(language, null, help.Catalogue.Find(language, "setup-checklist", new(["ops.settings.edit"], []))!.Metadata.Title);
        Assert.DoesNotContain(presenter.State.Articles, article => article.Metadata.Slug == "setup-checklist");
        Assert.Equal("/staff/help/workspace-at-a-glance", help.ContextAddress(language, "/staff", reader.Context.Reader));
        reader.Context = reader.Context with { Reader = new(["ops.settings.edit"], []) };
        await presenter.LoadAsync(language, "setup-checklist", null);
        Assert.False(presenter.State.NotFound);
        Assert.NotNull(presenter.State.Article);
    }

    [Fact]
    public async Task CompanyTokensAreEncodedAndNeverLeakIntoTheNextCompany()
    {
        OperationsHelpCatalogue help = Catalogue();
        OperationsHelpTestReader reader = new() { Context = new(new([], []), "<b>First & Co</b>", "https://first.test/customer?a=1&b=2") };
        using OperationsHelpPresenter presenter = new(help, reader, NullLogger<OperationsHelpPresenter>.Instance);
        await presenter.LoadAsync("en", "workspace-at-a-glance", null);
        Assert.Contains("&lt;b&gt;First &amp; Co&lt;/b&gt;", presenter.State.Html);
        Assert.DoesNotContain("<b>First", presenter.State.Html);
        reader.Context = new(new([], []), "Second Freight", "https://second.test/customer");
        await presenter.LoadAsync("en", "workspace-at-a-glance", null);
        Assert.Contains("Second Freight", presenter.State.Html);
        Assert.DoesNotContain("First", presenter.State.Html);
        Assert.Contains("{brandName}", help.Catalogue.Find("en", "workspace-at-a-glance", reader.Context.Reader)!.Html);
        reader.Context = reader.Context with { BrandName = "" };
        await presenter.LoadAsync("en", "workspace-at-a-glance", null);
        Assert.True(presenter.State.Unavailable);
        Assert.Empty(presenter.State.Html);
    }

    [Fact]
    public void UnknownPlanNeverEnablesHelpConditions()
    {
        Assert.Empty(OperationsHelpReaderSource.Conditions(OperationsPlanFeatures.Unknown));
        Assert.Equal(5, OperationsHelpReaderSource.Conditions(OperationsPlanFeatures.All).Count);
        Assert.Empty(OperationsHelpReaderSource.Conditions(new(false, false)));
    }
}
