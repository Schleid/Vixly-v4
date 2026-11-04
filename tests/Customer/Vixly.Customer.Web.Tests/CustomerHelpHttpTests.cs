using System.Net;
using Vixly.Contracts.Operations.Customers;
using Vixly.Contracts.Operations.Common;
using Vixly.Customer.Web.Hosting;
using Vixly.Customer.Web.Features.Help;

namespace Vixly.Customer.Web.Tests;

// =========================================================
// FILE: CustomerHelpHttpTests.cs
// PURPOSE:
// Verifies help through the real portal tenant and session pipeline.
// RESPONSIBILITIES:
// - Deny expired sessions and render company-owned localized guides.
// IMPORTANT NOTES:
// - The Operations transport is fake; its caller signatures are checked.
// =========================================================
public sealed class CustomerHelpHttpTests
{
    [Theory]
    [InlineData("en")][InlineData("fr")][InlineData("es")]
    public async Task VerifiedCustomerCanReadEveryPriorityGuide(string language)
    {
        await using CustomerWebTestHost host = await CustomerWebTestHost.StartAsync();
        host.Tenant("northwind", "live");
        host.Operations.Customers = call => call.Path == "me" && call.Session == "help-demo"
            ? (HttpStatusCode.OK, new CustomerProfileDto(Guid.NewGuid(), "C00042", "Ana", "Pierre", null, "ana@example.com", "2025550100", language, []))
            : (HttpStatusCode.Unauthorized, new OperationsApiErrorDto("ops.customers.session_required", new Dictionary<string, string>(), "help"));
        using HttpClient client = host.Client("northwind.techcrafters.ltd");
        client.DefaultRequestHeaders.Add("Cookie", CustomerCookies.Session + "=help-demo; " + CustomerCookies.Culture + "=c=" + language + "|uic=" + language);
        CustomerHelpCatalogue help = new();
        foreach (var article in help.Catalogue.Visible(language, new([], [])))
        {
            using HttpResponseMessage response = await client.GetAsync("/customer/help/" + article.Metadata.Slug);
            string html = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.Contains(article.Metadata.Title, WebUtility.HtmlDecode(html));
            Assert.Contains("Northwind Couriers", html);
            Assert.DoesNotContain("{brandName}", html);
            Assert.Contains("/customer/help/media/v2/" + language + "-", html);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/customer/help/absent")).StatusCode);
    }

    [Fact]
    public async Task SignedOutAndExpiredSessionsCannotReadPrivateGuides()
    {
        await using CustomerWebTestHost host = await CustomerWebTestHost.StartAsync();
        host.Tenant("northwind", "live");
        host.Operations.Customers = _ => (HttpStatusCode.Unauthorized, new OperationsApiErrorDto("ops.customers.session_required", new Dictionary<string, string>(), "help"));
        using HttpClient client = host.Client("northwind.techcrafters.ltd");
        string home = await client.GetStringAsync("/customer/help");
        Assert.Contains("/customer/sign-in", home);
        Assert.DoesNotContain("href=\"/customer/help/announce-a-package\"", home);
        foreach (string cookie in new[] { "__Host-vx4-ops=staff", CustomerCookies.Session + "=expired" })
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "/customer/help/announce-a-package");
            request.Headers.Add("Cookie", cookie);
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/customer/sign-in", response.Headers.Location?.OriginalString);
        }
    }

    [Fact]
    public async Task SessionServiceFailureDoesNotExposeHelpContent()
    {
        await using CustomerWebTestHost host = await CustomerWebTestHost.StartAsync();
        host.Tenant("northwind", "live");
        host.Operations.Customers = _ => (HttpStatusCode.ServiceUnavailable, null);
        using HttpClient client = host.Client("northwind.techcrafters.ltd");
        client.DefaultRequestHeaders.Add("Cookie", CustomerCookies.Session + "=help-demo");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/customer/help/create-a-shipment")).StatusCode);
    }
}
