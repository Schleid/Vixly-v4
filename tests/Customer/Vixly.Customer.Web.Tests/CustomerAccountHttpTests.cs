using System.Net;
using System.Text.RegularExpressions;
using Vixly.Contracts.Operations.Billing;
using Vixly.Contracts.Operations.Common;
using Vixly.Contracts.Operations.Customers;
using Vixly.Contracts.Operations.Packages;

namespace Vixly.Customer.Web.Tests;

// =========================================================
// FILE: CustomerAccountHttpTests.cs
//
// PURPOSE:
// Proves the customer portal pages over HTTP (Slice 2, design section 4).
//
// RESPONSIBILITIES:
// - Sign-up with the bot check, sign-in, the session cookie, the home page
// - Operations errors become the customer's own words, in the tenant brand
// - Antiforgery on every POST; no open redirect
//
// IMPORTANT NOTES:
// - Operations.Api is the fake handler; the real one is proved in the Operations tests
// =========================================================
public sealed partial class CustomerAccountHttpTests : IAsyncLifetime
{
    private const string Host = "northwind.techcrafters.ltd";

    private CustomerWebTestHost _host = null!;

    private readonly Dictionary<string, string> _cookies = new(StringComparer.Ordinal);

    public async Task InitializeAsync()
    {
        _host = await CustomerWebTestHost.StartAsync();
        _host.Tenant("northwind", "live");
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task SignedOut_HomeIsTheSignInPage_InTheTenantBrand_AndNeverVixly()
    {
        (HttpResponseMessage response, string html) = await GetAsync("/customer/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Northwind Couriers", html, StringComparison.Ordinal);
        Assert.Contains("action=\"/customer/sign-in\"", html, StringComparison.Ordinal);
        Assert.Contains("--customer-primary: #123456", html, StringComparison.Ordinal);
        Assert.True(response.Headers.CacheControl?.NoStore);
        AssertNoVixly(html);
    }

    // A failed request comes back to the error page with its own method, under the portal's path (2026-10-18).
    [Fact]
    public async Task ErrorPage_AnswersEveryMethod_InTheCustomersLanguage()
    {
        foreach (HttpMethod method in new[] { HttpMethod.Get, HttpMethod.Post })
        {
            HttpResponseMessage response = await SendAsync(method, "/customer/error", method == HttpMethod.Post ? new Dictionary<string, string>() : null);
            string html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("Something went wrong", html, StringComparison.Ordinal);
            Assert.Contains("href=\"/customer/\"", html, StringComparison.Ordinal);
            AssertNoVixly(System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", string.Empty));
        }
    }

    [Fact]
    public async Task Post_WithoutTheAntiforgeryToken_IsRefused()
    {
        HttpResponseMessage response = await SendAsync(HttpMethod.Post, "/customer/sign-out", new Dictionary<string, string>());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_SetsTheSessionCookie_AndHomeShowsTheMailbox()
    {
        // The first sign-in asks for the home address once (owner, 2026-10-26).
        CustomerAddressDto? given = null;
        _host.Operations.Customers = call => call switch
        {
            { Method: "POST", Path: "sessions" } when call.Field("password") == "Correct horse 42" => (HttpStatusCode.OK, new CustomerSessionDto("session-1", Ana())),
            { Method: "GET", Path: "me" } when call.Session == "session-1" => (HttpStatusCode.OK, Ana() with { HomeAddress = given }),
            { Method: "PUT", Path: "me/address" } when call.Session == "session-1" && call.Field("city") == "Les Cayes" =>
                (HttpStatusCode.OK, Ana() with { HomeAddress = given = new CustomerAddressDto(call.Field("line1"), null, "Les Cayes", "Sud", null, call.Field("countryCode")) }),
            { Method: "PUT", Path: "me/address" } => (HttpStatusCode.BadRequest, Error("ops.locations.city_required")),
            _ => (HttpStatusCode.Unauthorized, Error("ops.customers.session_required"))
        };

        HttpResponseMessage signIn = await PostFormAsync("/customer/sign-in", "/customer/sign-in", new() { ["email"] = "ana@example.com", ["password"] = "Correct horse 42" });

        Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);
        Assert.Equal("session-1", _cookies["__Secure-vx4-customer"]);

        (HttpResponseMessage first, _) = await GetAsync("/customer/");
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Equal("/customer/address", first.Headers.Location?.OriginalString);
        (_, string form) = await GetAsync("/customer/address");
        Assert.Contains("Before you start, tell us where you live", form, StringComparison.Ordinal);
        string refused = await ReadAsync(await PostFormAsync("/customer/address", "/customer/address", new() { ["line1"] = "12 Rue Geffrard", ["city"] = "", ["country"] = "HT" }));
        Assert.Contains("12 Rue Geffrard", refused, StringComparison.Ordinal);
        HttpResponseMessage saved = await PostFormAsync("/customer/address", "/customer/address", new() { ["line1"] = "12 Rue Geffrard", ["city"] = "Les Cayes", ["region"] = "Sud", ["country"] = "HT" });
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.Equal("HT", given?.CountryCode);

        (_, string home) = await GetAsync("/customer/");

        Assert.Contains("Hello Ana", home, StringComparison.Ordinal);
        Assert.Contains("Your customer code: C00042", home, StringComparison.Ordinal);
        Assert.Contains("Ana Pierre C00042", home, StringComparison.Ordinal);
        Assert.Contains("8400 NW 25th St", home, StringComparison.Ordinal);
        AssertNoVixly(home);
    }

    // Owner, 2026-10-27: the company sets the customers' inactivity sign-out; the portal warns with a countdown, then signs out.
    [Fact]
    public async Task Inactivity_FollowsTheCompanysTime_WarnsFirst_AndSignsOutWithAMessage()
    {
        _host.Operations.Brand = _host.Operations.Brand with { IdleMinutes = 90, IdleWarnMinutes = 3 };
        _host.Operations.Customers = call => call switch
        {
            { Method: "POST", Path: "sessions" } => (HttpStatusCode.OK, new CustomerSessionDto("session-9", Ana())),
            { Method: "GET", Path: "me" } when call.Session == "session-9" => (HttpStatusCode.OK, Ana() with { HomeAddress = new CustomerAddressDto("12 Rue Geffrard", null, "Les Cayes", "Sud", null, "HT") }),
            { Method: "DELETE" } => (HttpStatusCode.NoContent, null!),
            _ => (HttpStatusCode.Unauthorized, Error("ops.customers.session_required"))
        };

        (_, string signedOut) = await GetAsync("/customer/sign-in");
        Assert.DoesNotContain("data-idle-end", signedOut, StringComparison.Ordinal);

        await PostFormAsync("/customer/sign-in", "/customer/sign-in", new() { ["email"] = "ana@example.com", ["password"] = "Correct horse 42" });
        (_, string home) = await GetAsync("/customer/");
        Assert.Contains("data-idle-warn=\"5220\" data-idle-end=\"5400\"", home, StringComparison.Ordinal);
        Assert.Contains("js/customer-session.js", home, StringComparison.Ordinal);
        Assert.Contains("Stay signed in", home, StringComparison.Ordinal);
        Assert.Contains("data-sign-out", home, StringComparison.Ordinal);
        AssertNoVixly(home);

        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Get, "/customer/session/keepalive", null)).StatusCode);

        HttpResponseMessage idle = await PostFormAsync("/customer/", "/customer/sign-out", new() { ["reason"] = "idle" });
        Assert.Equal("/customer/sign-in?done=idle", idle.Headers.Location?.OriginalString);
        Assert.False(_cookies.ContainsKey("__Secure-vx4-customer"));
        (_, string message) = await GetAsync("/customer/sign-in?done=idle");
        Assert.Contains("You were signed out after a period of inactivity", message, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/customer/session/keepalive", null)).StatusCode);
    }

    // Owner, 2026-10-27: a Pay button for each way the company offers on the portal; Zelle and bank transfer are reported and wait for the company.
    [Fact]
    public async Task Invoices_OfferThePortalMethods_AndAReportedPaymentWaits()
    {
        Guid unpaid = Guid.NewGuid();
        Guid paid = Guid.NewGuid();
        bool offered = true;
        string? reported = null;
        _host.Operations.Customers = call => call switch
        {
            { Method: "POST", Path: "sessions" } => (HttpStatusCode.OK, new CustomerSessionDto("session-7", Ana())),
            { Method: "GET", Path: "me" } => (HttpStatusCode.OK, Ana() with { HomeAddress = new CustomerAddressDto("1 Rue A", null, "Les Cayes", "Sud", null, "HT") }),
            { Method: "GET", Path: "me/credit" } => (HttpStatusCode.OK, new CustomerCreditAccountDto(false, "none", 0m, 0, "USD", 0m, 0m, 0m, Array.Empty<CustomerCreditItemDto>())),
            { Method: "GET", Path: "me/invoices" } => (HttpStatusCode.OK, new CustomerInvoicesDto(new[]
            {
                new CustomerInvoiceDto(unpaid, "INV26000001", "shipment", "SH26000001", new DateOnly(2026, 10, 26), "USD", 20m, 0m, 20m, false, Array.Empty<CustomerCreditNoteDto>()),
                new CustomerInvoiceDto(paid, "INV26000002", "shipment", "SH26000002", new DateOnly(2026, 10, 26), "USD", 9m, 0m, 0m, false, Array.Empty<CustomerCreditNoteDto>(), "paid")
            }, false, offered, offered)),
            { Method: "GET" } when call.Path == "me/invoices/" + unpaid + "/pay/zelle" =>
                (HttpStatusCode.OK, new CustomerPayDto("INV26000001", "INV26000001", 20m, "USD", "Write the invoice number in the memo.", "@andes", null, "Andes Courier", false)),
            { Method: "POST" } when call.Path == "me/invoices/" + unpaid + "/pay/zelle" && call.Body?.GetProperty("termsAccepted").GetBoolean() != true =>
                (HttpStatusCode.BadRequest, Error("ops.payments.zelle_terms_required")),
            { Method: "POST" } when call.Path == "me/invoices/" + unpaid + "/pay/zelle" =>
                (HttpStatusCode.OK, new CustomerPayReportDto("INV26000001", reported = "PAY26000001")),
            _ => (HttpStatusCode.Unauthorized, Error("ops.customers.session_required"))
        };
        await PostFormAsync("/customer/sign-in", "/customer/sign-in", new() { ["email"] = "ana@example.com", ["password"] = "Correct horse 42" });

        (_, string list) = await GetAsync("/customer/invoices");
        Assert.Contains("href=\"/customer/invoices/" + unpaid + "/pay\"", list, StringComparison.Ordinal);
        Assert.DoesNotContain("/customer/invoices/" + paid + "/pay", list, StringComparison.Ordinal);

        (_, string choose) = await GetAsync("/customer/invoices/" + unpaid + "/pay");
        Assert.Contains("Pay by Zelle", choose, StringComparison.Ordinal);
        Assert.Contains("Pay by bank transfer", choose, StringComparison.Ordinal);
        Assert.DoesNotContain("Pay by card", choose, StringComparison.Ordinal);

        (_, string zelle) = await GetAsync("/customer/invoices/" + unpaid + "/pay/zelle");
        Assert.Contains("@andes", zelle, StringComparison.Ordinal);
        Assert.Contains("Andes Courier", zelle, StringComparison.Ordinal);
        Assert.Contains("Write the invoice number in the memo.", zelle, StringComparison.Ordinal);
        AssertNoVixly(zelle);

        string refused = await ReadAsync(await PostFormAsync("/customer/invoices/" + unpaid + "/pay/zelle", "/customer/invoices/" + unpaid + "/pay/zelle", new() { ["reference"] = "ZL-778" }));
        Assert.Contains("Accept the Zelle terms first.", refused, StringComparison.Ordinal);
        Assert.Contains("value=\"ZL-778\"", refused, StringComparison.Ordinal);

        HttpResponseMessage sent = await PostFormAsync("/customer/invoices/" + unpaid + "/pay/zelle", "/customer/invoices/" + unpaid + "/pay/zelle",
            new() { ["reference"] = "ZL-778", ["terms"] = "true" });
        Assert.Equal("/customer/invoices?done=reported&number=INV26000001", sent.Headers.Location?.OriginalString);
        Assert.Equal("PAY26000001", reported);
        (_, string after) = await GetAsync("/customer/invoices?done=reported&number=INV26000001");
        Assert.Contains("Your payment for invoice INV26000001 is waiting for confirmation", after, StringComparison.Ordinal);

        // Nothing offered on the portal: no Pay button.
        offered = false;
        (_, string none) = await GetAsync("/customer/invoices");
        Assert.DoesNotContain("/pay\"", none, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WrongPassword_ShowsOneMessage_AndUnconfirmedCanAskForANewLink()
    {
        _host.Operations.Customers = call => call.Field("email") == "new@example.com"
            ? (HttpStatusCode.Forbidden, Error("ops.customers.not_verified"))
            : (HttpStatusCode.BadRequest, Error("ops.customers.sign_in_failed"));

        string wrong = await ReadAsync(await PostFormAsync("/customer/sign-in", "/customer/sign-in", new() { ["email"] = "ana@example.com", ["password"] = "nope nope nope" }));
        string unconfirmed = await ReadAsync(await PostFormAsync("/customer/sign-in", "/customer/sign-in", new() { ["email"] = "new@example.com", ["password"] = "Correct horse 42" }));

        Assert.Contains("The email or password is not correct", wrong, StringComparison.Ordinal);
        Assert.DoesNotContain("action=\"/customer/verify/resend\"", wrong, StringComparison.Ordinal);
        Assert.Contains("Confirm your email first", unconfirmed, StringComparison.Ordinal);
        Assert.Contains("action=\"/customer/verify/resend\"", unconfirmed, StringComparison.Ordinal);
        Assert.False(_cookies.ContainsKey("__Secure-vx4-customer"));
    }

    [Fact]
    public async Task SignUp_RunsTheBotCheckFirst_ThenSendsToOperations()
    {
        _host.Operations.Customers = call => call.Path == "sign-up" ? (HttpStatusCode.Accepted, null) : (HttpStatusCode.NotFound, null);
        Dictionary<string, string> form = new()
        {
            ["firstName"] = "Ana",
            ["lastName"] = "Pierre",
            ["email"] = "ana@example.com",
            ["phone"] = "+50937123456",
            ["language"] = "fr",
            ["password"] = "Correct horse 42",
            ["cf-turnstile-response"] = "bot-token"
        };

        _host.Bot.Passes = false;
        string refused = await ReadAsync(await PostFormAsync("/customer/sign-up", "/customer/sign-up", form));

        Assert.Contains("We could not confirm that you are a person", refused, StringComparison.Ordinal);
        Assert.Contains("value=\"ana@example.com\"", refused, StringComparison.Ordinal);
        Assert.DoesNotContain(_host.Operations.Requests, call => call.Path == "sign-up");

        _host.Bot.Passes = true;
        HttpResponseMessage accepted = await PostFormAsync("/customer/sign-up", "/customer/sign-up", form);

        Assert.Equal("/customer/sign-up/sent", accepted.Headers.Location?.OriginalString);
        Assert.Equal("bot-token", _host.Bot.LastToken);
        FakeCustomerCall sent = Assert.Single(_host.Operations.Requests, call => call.Path == "sign-up");
        Assert.Equal("fr", sent.Field("language"));
        Assert.Equal("northwind", sent.Field("portalSlug"));
    }

    [Fact]
    public async Task PasswordRules_AreShownWithTheirValues()
    {
        _host.Operations.Customers = _ => (HttpStatusCode.BadRequest, new OperationsApiErrorDto("ops.auth.password_too_short", new Dictionary<string, string> { ["minLength"] = "12" }, "c1"));

        string html = await ReadAsync(await PostFormAsync("/customer/reset-password?token=t1", "/customer/reset-password", new() { ["token"] = "t1", ["password"] = "short" }));

        Assert.Contains("Use at least 12 characters.", html, StringComparison.Ordinal);
        Assert.Contains("value=\"t1\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VerifyLink_OnlyShowsAButton_UntilThePersonConfirms()
    {
        _host.Operations.Customers = call => call.Path == "verify" && call.Field("token") == "good"
            ? (HttpStatusCode.OK, new CustomerSessionDto("session-2", Ana()))
            : (HttpStatusCode.BadRequest, Error("ops.customers.link_invalid"));

        (_, string page) = await GetAsync("/customer/verify?token=good");

        Assert.Contains("value=\"good\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain(_host.Operations.Requests, call => call.Path == "verify");

        string dead = await ReadAsync(await PostFormAsync("/customer/verify?token=old", "/customer/verify", new() { ["token"] = "old" }));
        HttpResponseMessage good = await PostFormAsync("/customer/verify?token=good", "/customer/verify", new() { ["token"] = "good" });

        Assert.Contains("This link has expired or was already used", dead, StringComparison.Ordinal);
        Assert.Equal("/customer/?done=welcome", good.Headers.Location?.OriginalString);
        Assert.Equal("session-2", _cookies["__Secure-vx4-customer"]);
    }

    [Fact]
    public async Task RefusedSession_SignsTheCustomerOut()
    {
        _host.Operations.Customers = _ => (HttpStatusCode.Unauthorized, Error("ops.customers.session_required"));
        _cookies["__Secure-vx4-customer"] = "ended-session";

        (HttpResponseMessage response, string html) = await GetAsync("/customer/");

        Assert.Contains("action=\"/customer/sign-in\"", html, StringComparison.Ordinal);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith("__Secure-vx4-customer=;", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MalformedSessionCookie_IsIgnored()
    {
        _cookies["__Secure-vx4-customer"] = "not.a.session";

        (HttpResponseMessage response, string html) = await GetAsync("/customer/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("action=\"/customer/sign-in\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(_host.Operations.Requests, call => call.Path == "me");
    }

    [Theory]
    [InlineData("//evil.example/", "/customer/")]
    [InlineData("https://evil.example/", "/customer/")]
    [InlineData("/customer//evil.example", "/customer/")]
    [InlineData("/customer/profile", "/customer/profile")]
    public async Task LanguageSwitch_NeverLeavesThePortal(string returnUrl, string expected)
    {
        (HttpResponseMessage response, _) = await GetAsync("/customer/culture/fr?returnUrl=" + Uri.EscapeDataString(returnUrl));

        Assert.Equal(expected, response.Headers.Location?.OriginalString);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith("__Secure-vx4-customer-lang=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OperationsDown_SaysTheServiceIsNotAvailable()
    {
        _host.Operations.Customers = _ => (HttpStatusCode.InternalServerError, null);

        string html = await ReadAsync(await PostFormAsync("/customer/sign-in", "/customer/sign-in", new() { ["email"] = "ana@example.com", ["password"] = "Correct horse 42" }));

        Assert.Contains("The service is not available right now", html, StringComparison.Ordinal);
    }

    private static CustomerProfileDto Ana() => new(Guid.NewGuid(), "C00042", "Ana", "Pierre", null, "ana@example.com", "+50937123456", "en",
        new[] { new CustomerMailboxDto("Miami", "Ana Pierre C00042", "8400 NW 25th St", "Unit 7", "Doral", "FL", "33122", "US") });

    private static OperationsApiErrorDto Error(string code) => new(code, null, "c1");

    // Asset addresses name the shared UI library; the visible page must not.
    private static void AssertNoVixly(string html) =>
        Assert.DoesNotContain("vixly", AssetPath().Replace(html, string.Empty), StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("_content/Vixly\\.Shared\\.UI/[a-z.-]+|vixly-[a-z-]+\\.(css|js)|vx-[a-z_-]+", RegexOptions.IgnoreCase)]
    private static partial Regex AssetPath();

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"|type=\"hidden\" name=\"__RequestVerificationToken\" value=\"([^\"]+)\"|name=\"__RequestVerificationToken\" value=\"([^\"]+)\"")]
    private static partial Regex FormToken();

    private async Task<HttpResponseMessage> PostFormAsync(string pagePath, string postPath, Dictionary<string, string> form)
    {
        (_, string page) = await GetAsync(pagePath);
        Match token = FormToken().Match(page);
        Assert.True(token.Success, "The page has no antiforgery token.");
        Dictionary<string, string> fields = new(form) { ["__RequestVerificationToken"] = token.Groups.Values.Skip(1).First(group => group.Success).Value };
        return await SendAsync(HttpMethod.Post, postPath, fields);
    }

    private async Task<(HttpResponseMessage Response, string Html)> GetAsync(string path)
    {
        HttpResponseMessage response = await SendAsync(HttpMethod.Get, path, null);
        return (response, await response.Content.ReadAsStringAsync());
    }

    private static Task<string> ReadAsync(HttpResponseMessage response) => response.Content.ReadAsStringAsync();

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, Dictionary<string, string>? form)
    {
        using HttpRequestMessage request = new(method, path);

        if (form is not null)
        {
            request.Content = new FormUrlEncodedContent(form);
        }

        if (_cookies.Count > 0)
        {
            request.Headers.Add("Cookie", string.Join("; ", _cookies.Select(pair => pair.Key + "=" + pair.Value)));
        }

        HttpResponseMessage response = await _host.Client(Host).SendAsync(request);

        if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookies))
        {
            foreach (string cookie in setCookies)
            {
                string pair = cookie.Split(';')[0];
                int equals = pair.IndexOf('=', StringComparison.Ordinal);
                string name = pair[..equals];
                string value = pair[(equals + 1)..];

                if (value.Length == 0)
                {
                    _cookies.Remove(name);
                }
                else
                {
                    _cookies[name] = value;
                }
            }
        }

        return response;
    }
}
