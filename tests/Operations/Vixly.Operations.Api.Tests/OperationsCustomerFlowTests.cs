using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Customer.Web.Hosting;
using Vixly.Customer.Web.Operations;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Api.Tests;

// =========================================================
// FILE: OperationsCustomerFlowTests.cs
//
// PURPOSE:
// Proves Slice 2 end to end: the real customer portal, the real Operations.Api and a real database (design section 8).
//
// RESPONSIBILITIES:
// - Sign up, confirm, see the mailbox address with the code, sign out and in
// - The same answer for known and unknown emails
// - A suspended customer's session stops at the next call
// - Two tenants never meet; the same email may exist in both
//
// IMPORTANT NOTES:
// - Needs VIXLY_TEST_SQL_SERVER; the emails are kept by a fake mailer
// =========================================================
public sealed partial class OperationsCustomerFlowTests : IClassFixture<OperationsApiDatabase>, IAsyncLifetime
{
    private const string Password = "Correct horse 42";

    private readonly OperationsApiDatabase _database;

    private OperationsApiTestHost _api = null!;

    private WebApplication _portal = null!;

    private readonly Guid _north = Guid.NewGuid();

    private readonly Guid _south = Guid.NewGuid();

    public OperationsCustomerFlowTests(OperationsApiDatabase database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        if (_database.ConnectionString is null)
        {
            return;
        }

        _api = await OperationsApiTestHost.StartAsync(_database.ConnectionString);
        string north = "north" + _north.ToString("N")[..6];
        string south = "south" + _south.ToString("N")[..6];
        NorthSlug = north;
        SouthSlug = south;
        _api.Platform.Tenant(north, _north, "active");
        _api.Platform.Tenant(south, _south, "active");
        await SeedAsync(_north, "North Couriers", "8400 NW 25th St");
        await SeedAsync(_south, "South Envios", "100 Calle Sol");

        HttpMessageHandler operations = _api.Handler();
        _portal = CustomerWebHost.Build(
            new[]
            {
                "--environment=Production",
                "--applicationName=Vixly.Customer.Web",
                "--AllowedHosts=*",
                "--ConnectionStrings:Customer=Server=127.0.0.1,1;Database=None;User Id=none;Password=none;Connect Timeout=1;TrustServerCertificate=True",
                "--CustomerOperations:CallerKeyId=k1",
                "--CustomerOperations:CallerPrivateKey=" + _api.Customer.PrivateKey,
                "--CustomerBotCheck:Enabled=false",
                // Both hosts copy an appsettings.json into this bin folder; the portal's own values are given here so the copy order never matters.
                "--CustomerOperations:BaseUrl=https://" + OperationsApiTestHost.Host + "/",
                "--CustomerOperations:TimeoutSeconds=10",
                "--TenantHosting:BaseDomains:0=techcrafters.ltd"
            },
            builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                builder.Services.AddHttpClient(CustomerOperationsApi.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => operations);
            });
        await _portal.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database.ConnectionString is null)
        {
            return;
        }

        await _portal.StopAsync();
        await _portal.DisposeAsync();
        await _api.DisposeAsync();
    }

    private string NorthSlug { get; set; } = string.Empty;

    private string SouthSlug { get; set; } = string.Empty;

    [SqlServerFact]
    public async Task Customer_SignsUp_Confirms_AndSeesTheMailboxWithTheCode()
    {
        Browser ana = OpenBrowser(NorthSlug);

        HttpResponseMessage signUp = await ana.PostAsync("/customer/sign-up", "/customer/sign-up", SignUpForm("ana@example.com"));
        Assert.Equal("/customer/sign-up/sent", signUp.Headers.Location?.OriginalString);
        Assert.Equal("ana@example.com", Assert.Single(_api.Mailer.Sent).ToAddress);

        // Not confirmed yet: the right password says so, and only then.
        string early = await Read(await ana.PostAsync("/customer/sign-in", "/customer/sign-in", new() { ["email"] = "ana@example.com", ["password"] = Password }));
        Assert.Contains("Confirm your email first", early, StringComparison.Ordinal);

        string token = _api.Mailer.LastToken("ana@example.com");
        HttpResponseMessage verified = await ana.PostAsync("/customer/verify?token=" + Uri.EscapeDataString(token), "/customer/verify", new() { ["token"] = token });
        Assert.Equal("/customer/?done=welcome", verified.Headers.Location?.OriginalString);

        string home = await ana.GetStringAsync("/customer/?done=welcome");
        Match code = CustomerCode().Match(home);
        Assert.True(code.Success, "No customer code on the home page.");
        Assert.Contains("Ana Pierre " + code.Value, home, StringComparison.Ordinal);
        Assert.Contains("8400 NW 25th St", home, StringComparison.Ordinal);
        Assert.Contains("North Couriers", home, StringComparison.Ordinal);
        Assert.DoesNotContain("100 Calle Sol", home, StringComparison.Ordinal);

        // One mailbox per company (owner, 2026-10-21): a second receiving warehouse shows nowhere until the company names it the mailbox; then only it shows.
        await WithDatabaseAsync(_north, async context =>
        {
            OperationsBranch branch = await context.Branches.FirstAsync();
            OperationsWarehouse fortLauderdale = OperationsWarehouse.Create(_north, branch.Id, new OperationsLocationDetails("FLL", "Fort Lauderdale",
                OperationsPostalAddress.Create("500 SW 1st Ave", null, "Fort Lauderdale", "FL", "33301", "US"), null, null, null, true), OperationsWarehouseRoles.Receiving, true, DateTimeOffset.UtcNow);
            context.Add(fortLauderdale);
        });
        Assert.DoesNotContain("500 SW 1st Ave", await ana.GetStringAsync("/customer/"), StringComparison.Ordinal);
        await WithDatabaseAsync(_north, async context =>
        {
            OperationsWarehouse fortLauderdale = await context.Warehouses.SingleAsync(item => item.Code == "FLL");
            fortLauderdale.SetMailbox(true, DateTimeOffset.UtcNow);

            // Only an active receiving warehouse that accepts deliveries can be the mailbox; one that stops receiving stops being it.
            fortLauderdale.Update(fortLauderdale.BranchId, new OperationsLocationDetails("FLL", "Fort Lauderdale", fortLauderdale.Address, null, null, null, true),
                OperationsWarehouseRoles.Storage, false, OperationsLocationStatus.Active, DateTimeOffset.UtcNow);
            Assert.False(fortLauderdale.IsMailbox);
            Assert.Equal(OperationsLocationErrors.MailboxInvalid, Assert.Throws<Vixly.Shared.Kernel.Domain.DomainException>(() => fortLauderdale.SetMailbox(true, DateTimeOffset.UtcNow)).Error);
            fortLauderdale.Update(fortLauderdale.BranchId, new OperationsLocationDetails("FLL", "Fort Lauderdale", fortLauderdale.Address, null, null, null, true),
                OperationsWarehouseRoles.Receiving, true, OperationsLocationStatus.Active, DateTimeOffset.UtcNow);
            fortLauderdale.SetMailbox(true, DateTimeOffset.UtcNow);
        });
        string moved = await ana.GetStringAsync("/customer/");
        Assert.Contains("500 SW 1st Ave", moved, StringComparison.Ordinal);
        Assert.DoesNotContain("8400 NW 25th St", moved, StringComparison.Ordinal);

        // The same link works once.
        string again = await Read(await ana.PostAsync("/customer/verify?token=" + Uri.EscapeDataString(token), "/customer/verify", new() { ["token"] = token }));
        Assert.Contains("This link has expired or was already used", again, StringComparison.Ordinal);

        // Sign out, then in with the password.
        await ana.PostAsync("/customer/", "/customer/sign-out", new());
        Assert.Contains("action=\"/customer/sign-in\"", await ana.GetStringAsync("/customer/"), StringComparison.Ordinal);
        HttpResponseMessage signIn = await ana.PostAsync("/customer/sign-in", "/customer/sign-in", new() { ["email"] = "ana@example.com", ["password"] = Password });
        Assert.Equal("/customer/", signIn.Headers.Location?.OriginalString);
        Assert.Contains(code.Value, await ana.GetStringAsync("/customer/"), StringComparison.Ordinal);
    }

    // Review item 3: a work email at any company never opens a customer account; the screen answers as always, the email says why.
    [SqlServerFact]
    public async Task SignUp_WithAWorkEmailOfAnyCompany_OpensNoAccount()
    {
        await WithDatabaseAsync(_south, context =>
        {
            // Only a member who accepted the invitation holds the email; one never accepted blocks nobody (review).
            OperationsUser wes = OperationsUser.InviteStaff(_south, "Wes Worker", EmailAddress.Create("wes@south-staff.example"), LanguageCode.En, DateTimeOffset.UtcNow);
            wes.AcceptInvitation("$2a$12$hash", DateTimeOffset.UtcNow);
            context.Add(wes);
            context.Add(OperationsUser.InviteStaff(_south, "Ida Invited", EmailAddress.Create("ida@south-staff.example"), LanguageCode.En, DateTimeOffset.UtcNow));
            return Task.CompletedTask;
        });
        Browser stranger = OpenBrowser(NorthSlug);
        _api.Mailer.Sent.Clear();

        HttpResponseMessage work = await stranger.PostAsync("/customer/sign-up", "/customer/sign-up", SignUpForm("wes@south-staff.example"));
        HttpResponseMessage fresh = await stranger.PostAsync("/customer/sign-up", "/customer/sign-up", SignUpForm("wes.home@example.com"));
        await stranger.PostAsync("/customer/sign-up", "/customer/sign-up", SignUpForm("ida@south-staff.example"));

        Assert.Equal(fresh.Headers.Location, work.Headers.Location);
        Assert.Equal(OperationsCustomerMailKind.EmailNotAllowed, _api.Mailer.Sent.Single(mail => mail.ToAddress == "wes@south-staff.example").Kind);
        Assert.Equal(OperationsCustomerMailKind.Verification, _api.Mailer.Sent.Single(mail => mail.ToAddress == "wes.home@example.com").Kind);
        Assert.Equal(OperationsCustomerMailKind.Verification, _api.Mailer.Sent.Single(mail => mail.ToAddress == "ida@south-staff.example").Kind);
        await WithDatabaseAsync(_north, async context =>
            Assert.False(await context.Set<OperationsCustomer>().IgnoreQueryFilters().AnyAsync(customer => customer.Email == EmailAddress.Create("wes@south-staff.example"))));
    }

    [SqlServerFact]
    public async Task KnownAndUnknownEmails_GetTheSameAnswer()
    {
        Browser ana = await ConfirmedAsync(NorthSlug, "known@example.com");
        Browser stranger = OpenBrowser(NorthSlug);
        _api.Mailer.Sent.Clear();

        HttpResponseMessage known = await stranger.PostAsync("/customer/sign-up", "/customer/sign-up", SignUpForm("known@example.com"));
        HttpResponseMessage fresh = await stranger.PostAsync("/customer/sign-up", "/customer/sign-up", SignUpForm("fresh@example.com"));
        HttpResponseMessage forgotKnown = await stranger.PostAsync("/customer/forgot-password", "/customer/forgot-password", new() { ["email"] = "known@example.com" });
        HttpResponseMessage forgotUnknown = await stranger.PostAsync("/customer/forgot-password", "/customer/forgot-password", new() { ["email"] = "nobody@example.com" });

        Assert.Equal(known.Headers.Location, fresh.Headers.Location);

        // A refused field gives the same answer too: the fields are checked before the email (review M1).
        Dictionary<string, string> badPhone = SignUpForm("known@example.com");
        badPhone["phone"] = "abc";
        string knownBad = await Read(await stranger.PostAsync("/customer/sign-up", "/customer/sign-up", badPhone));
        badPhone["email"] = "fresh2@example.com";
        string freshBad = await Read(await stranger.PostAsync("/customer/sign-up", "/customer/sign-up", badPhone));
        Assert.Contains("Enter the phone with the country code", knownBad, StringComparison.Ordinal);
        Assert.Contains("Enter the phone with the country code", freshBad, StringComparison.Ordinal);
        Assert.Equal(forgotKnown.Headers.Location, forgotUnknown.Headers.Location);
        Assert.Equal(OperationsCustomerMailKind.AccountExists, _api.Mailer.Sent.Single(mail => mail.ToAddress == "known@example.com" && mail.Kind != OperationsCustomerMailKind.PasswordReset).Kind);
        Assert.DoesNotContain(_api.Mailer.Sent, mail => mail.ToAddress == "nobody@example.com");

        // The reset link signs in with the new password and ends the old session.
        string token = _api.Mailer.LastToken("known@example.com");
        HttpResponseMessage reset = await stranger.PostAsync("/customer/reset-password?token=" + Uri.EscapeDataString(token), "/customer/reset-password", new() { ["token"] = token, ["password"] = "Another horse 77" });
        Assert.Equal("/customer/?done=saved", reset.Headers.Location?.OriginalString);
        Assert.Contains("action=\"/customer/sign-in\"", await ana.GetStringAsync("/customer/"), StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task NewPassword_CancelsOlderLinks_AndWrongCurrentPasswordsLockTheAccount()
    {
        Browser ana = await ConfirmedAsync(NorthSlug, "links@example.com");
        await OpenBrowser(NorthSlug).PostAsync("/customer/forgot-password", "/customer/forgot-password", new() { ["email"] = "links@example.com" });
        string older = _api.Mailer.LastToken("links@example.com");

        HttpResponseMessage changed = await ana.PostAsync("/customer/profile", "/customer/profile/password", new() { ["currentPassword"] = Password, ["newPassword"] = "Another horse 77" });
        Assert.Equal("/customer/profile?done=1", changed.Headers.Location?.OriginalString);

        // The reset link asked for before the change no longer works (review L3).
        string dead = await Read(await OpenBrowser(NorthSlug).PostAsync("/customer/reset-password?token=" + Uri.EscapeDataString(older), "/customer/reset-password",
            new() { ["token"] = older, ["password"] = "Third horse 99 x" }));
        Assert.Contains("This link has expired or was already used", dead, StringComparison.Ordinal);

        // Five wrong current passwords lock the account and end the session (review L4).
        for (int attempt = 0; attempt < 5; attempt++)
        {
            await ana.PostAsync("/customer/profile", "/customer/profile/password", new() { ["currentPassword"] = "wrong password " + attempt, ["newPassword"] = "Fourth horse 11" });
        }

        Assert.Contains("action=\"/customer/sign-in\"", await ana.GetStringAsync("/customer/"), StringComparison.Ordinal);
        string locked = await Read(await OpenBrowser(NorthSlug).PostAsync("/customer/sign-in", "/customer/sign-in", new() { ["email"] = "links@example.com", ["password"] = "Another horse 77" }));
        Assert.Contains("locked for a few minutes", locked, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task SuspendedCustomer_IsSignedOutAtTheNextCall()
    {
        Browser ana = await ConfirmedAsync(NorthSlug, "suspend@example.com");
        Assert.Contains("Hello Ana", await ana.GetStringAsync("/customer/"), StringComparison.Ordinal);

        await WithDatabaseAsync(_north, async context =>
        {
            OperationsCustomer customer = await context.Customers.SingleAsync(item => item.Email == Vixly.Shared.Kernel.ValueObjects.EmailAddress.Create("suspend@example.com"));
            customer.ChangeStatus(OperationsCustomerStatus.Suspended, "Unpaid shipments", DateTimeOffset.UtcNow);
        });

        Assert.Contains("action=\"/customer/sign-in\"", await ana.GetStringAsync("/customer/"), StringComparison.Ordinal);
        string again = await Read(await ana.PostAsync("/customer/sign-in", "/customer/sign-in", new() { ["email"] = "suspend@example.com", ["password"] = Password }));
        Assert.Contains("The email or password is not correct", again, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task TwoTenants_NeverMeet_AndTheSameEmailMayExistInBoth()
    {
        Browser north = await ConfirmedAsync(NorthSlug, "both@example.com");
        Browser south = await ConfirmedAsync(SouthSlug, "both@example.com");

        Assert.Contains("8400 NW 25th St", await north.GetStringAsync("/customer/"), StringComparison.Ordinal);
        Assert.Contains("100 Calle Sol", await south.GetStringAsync("/customer/"), StringComparison.Ordinal);

        // North's session cookie on South's host is not a session there.
        Browser thief = OpenBrowser(SouthSlug);
        thief.Cookies["__Secure-vx4-customer"] = north.Cookies["__Secure-vx4-customer"];
        Assert.Contains("action=\"/customer/sign-in\"", await thief.GetStringAsync("/customer/"), StringComparison.Ordinal);

        // A customer of North only cannot sign in to South.
        await ConfirmedAsync(NorthSlug, "northonly@example.com");
        string refused = await Read(await OpenBrowser(SouthSlug).PostAsync("/customer/sign-in", "/customer/sign-in", new() { ["email"] = "northonly@example.com", ["password"] = Password }));
        Assert.Contains("The email or password is not correct", refused, StringComparison.Ordinal);
    }

    private async Task<Browser> ConfirmedAsync(string slug, string email)
    {
        Browser browser = OpenBrowser(slug);
        await browser.PostAsync("/customer/sign-up", "/customer/sign-up", SignUpForm(email));
        string token = _api.Mailer.LastToken(email);
        HttpResponseMessage verified = await browser.PostAsync("/customer/verify?token=" + Uri.EscapeDataString(token), "/customer/verify", new() { ["token"] = token });
        Assert.Equal(HttpStatusCode.Redirect, verified.StatusCode);
        return browser;
    }

    private static Dictionary<string, string> SignUpForm(string email) => new()
    {
        ["firstName"] = "Ana",
        ["lastName"] = "Pierre",
        ["email"] = email,
        ["phone"] = "+50937123456",
        ["language"] = "en",
        ["password"] = Password
    };

    private async Task SeedAsync(Guid tenantId, string name, string street)
    {
        await WithDatabaseAsync(tenantId, context =>
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            OperationsTenantBrand brand = OperationsTenantBrand.Create(tenantId, name, now);
            brand.Publish(now);
            OperationsBranch branch = OperationsBranch.Create(tenantId, Details("MIA", "Miami", street), OperationsBranchSide.Origin, now);
            context.Add(brand);
            context.Add(branch);
            context.Add(OperationsWarehouse.Create(tenantId, branch.Id, Details("WH1", "Receiving", street), OperationsWarehouseRoles.Receiving, true, now));
            return Task.CompletedTask;
        });
    }

    private static OperationsLocationDetails Details(string code, string name, string street) =>
        new(code, name, OperationsPostalAddress.Create(street, null, "Doral", "FL", "33122", "US"), null, null, null, true);

    private async Task WithDatabaseAsync(Guid tenantId, Func<OperationsDbContext, Task> work)
    {
        await using AsyncServiceScope scope = _api.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenantId);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        await work(context);
        await context.SaveChangesAsync();
    }

    private Browser OpenBrowser(string slug) => new(new HttpClient(_portal.GetTestServer().CreateHandler()) { BaseAddress = new Uri($"https://{slug}.techcrafters.ltd/") });

    private static Task<string> Read(HttpResponseMessage response) => response.Content.ReadAsStringAsync();

    [GeneratedRegex("C\\d{5,}")]
    private static partial Regex CustomerCode();

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"|type=\"hidden\" name=\"__RequestVerificationToken\" value=\"([^\"]+)\"|name=\"__RequestVerificationToken\" value=\"([^\"]+)\"")]
    private static partial Regex FormToken();

    // A browser with a cookie jar that never follows redirects.
    private sealed class Browser
    {
        private readonly HttpClient _client;

        public Browser(HttpClient client)
        {
            _client = client;
        }

        public Dictionary<string, string> Cookies { get; } = new(StringComparer.Ordinal);

        public async Task<string> GetStringAsync(string path) => await (await SendAsync(HttpMethod.Get, path, null)).Content.ReadAsStringAsync();

        public async Task<HttpResponseMessage> PostAsync(string pagePath, string postPath, Dictionary<string, string> form)
        {
            string page = await GetStringAsync(pagePath);
            Match token = FormToken().Match(page);
            Assert.True(token.Success, "The page " + pagePath + " has no antiforgery token.");
            return await SendAsync(HttpMethod.Post, postPath, new Dictionary<string, string>(form) { ["__RequestVerificationToken"] = token.Groups.Values.Skip(1).First(group => group.Success).Value });
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, Dictionary<string, string>? form)
        {
            using HttpRequestMessage request = new(method, path);

            if (form is not null)
            {
                request.Content = new FormUrlEncodedContent(form);
            }

            if (Cookies.Count > 0)
            {
                request.Headers.Add("Cookie", string.Join("; ", Cookies.Select(pair => pair.Key + "=" + pair.Value)));
            }

            HttpResponseMessage response = await _client.SendAsync(request);

            foreach (string cookie in response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values) ? values : Array.Empty<string>())
            {
                string pair = cookie.Split(';')[0];
                int equals = pair.IndexOf('=', StringComparison.Ordinal);

                if (pair[(equals + 1)..].Length == 0)
                {
                    Cookies.Remove(pair[..equals]);
                }
                else
                {
                    Cookies[pair[..equals]] = pair[(equals + 1)..];
                }
            }

            return response;
        }
    }
}
