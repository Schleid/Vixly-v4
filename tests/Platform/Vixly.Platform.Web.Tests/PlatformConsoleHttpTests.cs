using System.Net;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: PlatformConsoleHttpTests.cs
//
// PURPOSE:
// Proves the console's HTTP defences on the real pipeline
// (Slice 1 "Done when": login CSRF, rate limits, headers).
//
// RESPONSIBILITIES:
// - A sign-in post without the antiforgery token is refused
// - The eleventh sign-in post in a minute from one address
//   answers 429
// - Every response carries the security headers
// - A protected page sends an anonymous visitor to sign-in
//
// IMPORTANT NOTES:
// - No database: see PlatformConsoleHost
// =========================================================
public sealed class PlatformConsoleHttpTests
{
    private static FormUrlEncodedContent SignInForm => new(new Dictionary<string, string>
    {
        ["email"] = "ada@example.com",
        ["password"] = "orbit-lantern-quarry-42",
        ["returnUrl"] = "/"
    });

    // A failed request comes back to the error page with its own method; a failed form post used to meet a GET-only page (2026-10-18).
    [Fact]
    public async Task ErrorPage_AnswersEveryMethod()
    {
        await using PlatformConsoleHost host = await PlatformConsoleHost.StartAsync();

        foreach (HttpMethod method in new[] { HttpMethod.Get, HttpMethod.Post })
        {
            using HttpRequestMessage request = new(method, "/error");
            HttpResponseMessage response = await host.Client().SendAsync(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("Something went wrong", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task SignIn_WithoutTheAntiforgeryToken_IsRefused()
    {
        await using PlatformConsoleHost host = await PlatformConsoleHost.StartAsync();

        HttpResponseMessage response = await host.Client().PostAsync("/sign-in", SignInForm);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_TheEleventhPostInAMinute_IsRateLimited()
    {
        await using PlatformConsoleHost host = await PlatformConsoleHost.StartAsync();
        HttpClient client = host.Client();
        List<HttpStatusCode> codes = new();

        for (int attempt = 0; attempt < 11; attempt++)
        {
            codes.Add((await client.PostAsync("/sign-in", SignInForm)).StatusCode);
        }

        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, codes.Take(10));
        Assert.Equal(HttpStatusCode.TooManyRequests, codes[10]);
    }

    [Fact]
    public async Task SignInPage_CarriesTheSecurityHeaders_AndAnAntiforgeryToken()
    {
        await using PlatformConsoleHost host = await PlatformConsoleHost.StartAsync();

        HttpResponseMessage response = await host.Client().GetAsync("/sign-in");
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("frame-ancestors 'none'", Header(response, "Content-Security-Policy"), StringComparison.Ordinal);
        Assert.Contains("script-src 'self'", Header(response, "Content-Security-Policy"), StringComparison.Ordinal);
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
        Assert.Equal("strict-origin-when-cross-origin", Header(response, "Referrer-Policy"));
        Assert.Contains("__RequestVerificationToken", page, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/users")]
    [InlineData("/tenants")]
    [InlineData("/tenants/registrations")]
    [InlineData("/leads")]
    [InlineData("/tenants/changes")]
    public async Task ProtectedPage_SendsAnAnonymousVisitorToSignIn(string path)
    {
        await using PlatformConsoleHost host = await PlatformConsoleHost.StartAsync();

        HttpResponseMessage response = await host.Client().GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Uri location = response.Headers.Location!;
        Assert.StartsWith("/sign-in", location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString, StringComparison.Ordinal);
        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
    }

    private static string Header(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out IEnumerable<string>? values) ? string.Join(",", values) : string.Empty;
    }
}
