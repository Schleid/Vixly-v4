using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: PlatformRenderOnceHttpTests.cs
//
// PURPOSE:
// Proves harmonization H-8 (owner, 2026-11-04): Control's signed-in pages render once, in the live connection.
//
// RESPONSIBILITIES:
// - A signed-in page arrives without the first render on the server
// - Help pages keep it (help answers a hidden article with a real 404); the sign-in page is still served whole by the server
//
// IMPORTANT NOTES:
// - Read from the Blazor marker: a first render on the server carries a prerender id
// =========================================================
public sealed class PlatformRenderOnceHttpTests
{
    [Fact]
    public async Task SignedInPages_RenderOnce_HelpAndSignInKeepTheFirstRender()
    {
        await using PlatformConsoleHost host = await PlatformConsoleHost.StartAsync(services =>
        {
            services.AddSingleton<IPlatformPermissionReader>(new PlatformHelpTestPermissions { SuperUser = true });
            services.AddAuthentication(options => options.DefaultAuthenticateScheme = "help-tests")
                .AddScheme<AuthenticationSchemeOptions, PlatformHelpTestAccess>("help-tests", _ => { });
        });
        using HttpClient client = host.Client();

        string overview = await client.GetStringAsync("/");
        Assert.Contains("<!--Blazor:", overview, StringComparison.Ordinal);
        Assert.DoesNotContain("prerenderId", overview, StringComparison.Ordinal);

        // The page's own sign-out form, written by the server with its token, for the sidebar button and the idle sign-out.
        System.Text.RegularExpressions.Match form = System.Text.RegularExpressions.Regex.Match(overview,
            "<form id=\"vx-sign-out\"[^>]*action=\"/sign-out\"[^>]*>\\s*<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"");
        Assert.True(form.Success && form.Groups["token"].Value.Length > 20, "the sign-out form and its token");

        string help = await client.GetStringAsync("/help");
        Assert.Contains("prerenderId", help, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/help/unknown-help-slug")).StatusCode);

        await using PlatformConsoleHost signedOut = await PlatformConsoleHost.StartAsync();
        // The sign-in page is served whole by the server, its form in the first response.
        Assert.Contains("<form", await signedOut.Client().GetStringAsync("/sign-in"), StringComparison.Ordinal);
    }
}
