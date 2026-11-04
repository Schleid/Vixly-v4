using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Web.Features.Help;
using Vixly.Shared.Help;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: PlatformHelpHttpTests.cs
//
// PURPOSE:
// Verifies Control help on the real authenticated HTTP pipeline.
//
// RESPONSIBILITIES:
// - Prove language reachability and publication route validation.
// - Prove hiding on home, search, related and direct requests.
//
// IMPORTANT NOTES:
// - Uses synthetic authentication and the real access use case.
// - Does not connect to a database or alter business records.
// =========================================================
public sealed class PlatformHelpHttpTests
{
    [Theory]
    [InlineData("/help")]
    [InlineData("/help/control-at-a-glance")]
    public async Task AnonymousReadersAreSentToSignIn(string path)
    {
        await using PlatformConsoleHost host = await PlatformConsoleHost.StartAsync();
        using HttpResponseMessage response = await host.Client().GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/sign-in", response.Headers.Location?.OriginalString, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    public async Task EveryArticleOpensForSuperUserAndRoutesAreReal(string language)
    {
        await using PlatformConsoleHost host = await StartAsync(new() { SuperUser = true });
        PlatformHelpCatalogue help = new();
        Assert.Empty(HelpCheck.Run(help.Documents, PlatformHelpCatalogue.Options(host.Routes)));
        using HttpClient client = host.Client();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
        using HttpResponseMessage home = await client.GetAsync("/help");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains(".styles.css", await home.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        foreach (HelpArticle article in help.Catalogue.Visible(language, new([], [], superUser: true)))
        {
            using HttpResponseMessage response = await client.GetAsync("/help/" + article.Metadata.Slug);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Assert.Contains(article.Metadata.Title, html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task PermissionGroupsAreHiddenFromHomeSearchAndDirectAddresses()
    {
        PlatformHelpCatalogue help = new();
        PlatformHelpTestPermissions access = new();
        await using PlatformConsoleHost host = await StartAsync(access);
        using HttpClient client = host.Client();
        string home = await client.GetStringAsync("/help");
        foreach (var group in help.Catalogue.Visible("en", new([], [], superUser: true))
            .Where(article => article.Metadata.Permissions.Count > 0)
            .GroupBy(article => article.Metadata.Permissions[0]))
        {
            foreach (HelpArticle article in group)
            {
                Assert.DoesNotContain("/help/" + article.Metadata.Slug + "\"", home, StringComparison.Ordinal);
                string search = await client.GetStringAsync("/help?q=" + Uri.EscapeDataString(article.Metadata.Title));
                Assert.DoesNotContain("/help/" + article.Metadata.Slug + "\"", search, StringComparison.Ordinal);
                using HttpResponseMessage hidden = await client.GetAsync("/help/" + article.Metadata.Slug);
                Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
            }
            if (group.Key == "superuser") continue;
            access.Codes = new(group.First().Metadata.Permissions, StringComparer.Ordinal);
            using HttpResponseMessage allowed = await client.GetAsync("/help/" + group.First().Metadata.Slug);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            access.Codes.Clear();
        }
    }

    [Fact]
    public async Task RevokedOrFailedPermissionsDoNotLeakRelatedArticlesOrContext()
    {
        PlatformHelpTestPermissions access = new() { Codes = new() { PlatformPermissions.TenantsView } };
        await using PlatformConsoleHost host = await StartAsync(access);
        using HttpClient client = host.Client();
        string html = await client.GetStringAsync("/help/company-page");
        Assert.DoesNotContain("/help/edit-company-details\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/help?q=", html, StringComparison.Ordinal);
        access.Fails = true;
        using HttpResponseMessage denied = await client.GetAsync("/help/company-page");
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/help/control-at-a-glance")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/help/unknown-help-slug")).StatusCode);
    }

    private static Task<PlatformConsoleHost> StartAsync(PlatformHelpTestPermissions access) =>
        PlatformConsoleHost.StartAsync(services =>
        {
            services.AddSingleton<IPlatformPermissionReader>(access);
            services.AddAuthentication(options => options.DefaultAuthenticateScheme = "help-tests")
                .AddScheme<AuthenticationSchemeOptions, PlatformHelpTestAccess>("help-tests", _ => { });
        });
}
