using Microsoft.Extensions.Logging.Abstractions;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Web.Features.Help;
using Vixly.Shared.Help;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsHelpSetupTests.cs
//
// PURPOSE:
// Covers the H4a reader groups and contextual setup destinations.
//
// RESPONSIBILITIES:
// - Deny hidden guides through direct links, search and related links.
// - Open every localized article for a fully allowed reader.
//
// IMPORTANT NOTES:
// - Menu grants are already resolved by the production reader source.
// =========================================================
public sealed class OperationsHelpSetupTests
{
    [Theory]
    [InlineData("ops.settings.view", "invoice-settings")]
    [InlineData("ops.branches.view", "branches-and-warehouses")]
    [InlineData("ops.warehouses.view", "branches-and-warehouses")]
    [InlineData("ops.warehouses.manage", "storage-locations")]
    [InlineData("ops.printers.manage", "printers")]
    [InlineData("ops.services.view", "shipping-services")]
    [InlineData("ops.charges.configure", "price-lists")]
    [InlineData("ops.insurance.view", "insurance-setup")]
    [InlineData("ops.inspection.configure", "inspection-rules")]
    [InlineData("ops.carriers.manage", "carriers")]
    public async Task SetupGuidesRespectEachReaderGroup(string permission, string slug)
    {
        OperationsHelpCatalogue help = OperationsHelpTestCatalogue.Create();
        OperationsHelpTestReader source = new();
        using OperationsHelpPresenter presenter = new(help, source, NullLogger<OperationsHelpPresenter>.Instance);
        foreach (string language in new[] { "en", "fr", "es" })
        {
            source.Context = source.Context with { Reader = new([], []) };
            HelpArticle article = help.Catalogue.Find(language, slug, new([permission], []))!;
            Assert.NotNull(article);
            await presenter.LoadAsync(language, slug, null);
            Assert.True(presenter.State.NotFound);
            await presenter.LoadAsync(language, null, article.Metadata.Title);
            Assert.DoesNotContain(presenter.State.Articles, item => item.Metadata.Slug == slug);
            Assert.Null(help.Catalogue.ForRoute(language, article.Metadata.Routes.FirstOrDefault() ?? "/absent", source.Context.Reader));
            source.Context = source.Context with { Reader = new(["ops.settings.edit"], []) };
            await presenter.LoadAsync(language, "setup-checklist", null);
            Assert.DoesNotContain(presenter.State.Related, item => item.Metadata.Slug == slug);
            source.Context = source.Context with { Reader = new([permission], []) };
            await presenter.LoadAsync(language, slug, null);
            Assert.Equal(slug, presenter.State.Article?.Metadata.Slug);
            foreach (string route in article.Metadata.Routes)
                Assert.Equal(OperationsHelpCatalogue.Address(language, slug), help.ContextAddress(language, route, source.Context.Reader));
        }
    }

    [Theory]
    [InlineData("en")][InlineData("fr")][InlineData("es")]
    public async Task EverySetupGuideOpensWithCompanyBranding(string language)
    {
        OperationsHelpCatalogue help = OperationsHelpTestCatalogue.Create();
        OperationsHelpTestReader source = new() { Context = new(new(OperationsPermissions.All, []), "Example Freight", "https://example.test/customer") };
        using OperationsHelpPresenter presenter = new(help, source, NullLogger<OperationsHelpPresenter>.Instance);
        foreach (HelpArticle article in help.Catalogue.Visible(language, source.Context.Reader))
        {
            await presenter.LoadAsync(language, article.Metadata.Slug, null);
            Assert.False(presenter.State.NotFound);
            Assert.Contains("Example Freight", presenter.State.Html);
            Assert.DoesNotContain("{brandName}", presenter.State.Html);
            Assert.DoesNotContain("Vixly", presenter.State.Html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ChecklistOnlyLinksToPublishedSetupTopics()
    {
        OperationsHelpCatalogue help = OperationsHelpTestCatalogue.Create();
        foreach (string code in new[] { "settings", "branch", "receiving_warehouse", "services" })
            Assert.NotNull(help.Catalogue.Find("en", OperationsSetupHelpTopics.ForChecklist(code), new(OperationsPermissions.All, [])));
        Assert.Empty(OperationsSetupHelpTopics.ForChecklist("site"));
        Assert.Empty(OperationsSetupHelpTopics.ForChecklist("staff"));
    }
}
