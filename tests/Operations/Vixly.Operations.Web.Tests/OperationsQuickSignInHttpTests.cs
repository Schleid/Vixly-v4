using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using OtpNet;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsQuickSignInHttpTests.cs
//
// PURPOSE:
// Badge and PIN sign-in through the pages (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Print a badge; sign in with it on the licensed device: the code once a day, then the badge alone
// - The device then opens at the badge screen; a badge session never counts as a fresh code
//
// IMPORTANT NOTES:
// - SQL Server and the fake Platform; the badge's code is set to a known one in the database after printing
// =========================================================
public sealed class OperationsQuickSignInHttpTests : IClassFixture<OperationsWebDatabase>, IAsyncLifetime
{
    private const string Password = "harbour lights at dawn";

    private const string BadgeCode = "SBABCDEFGHJKLMNPQRSTUVWXYZ2";

    private static readonly Regex ManualKey = new("<code>(?<key>[A-Z2-7]{16,})</code>", RegexOptions.CultureInvariant);

    private readonly OperationsWebDatabase _database;

    private OperationsWebTestHost _host = null!;

    public OperationsQuickSignInHttpTests(OperationsWebDatabase database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        _host = await OperationsWebTestHost.StartAsync(_database.ConnectionString);
        _host.Devices.AutoApprove = true;
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [SqlServerFact]
    public async Task Badge_SignsInOnTheLicensedDevice_WithTheCodeOnceADay()
    {
        Guid tenant = NewTenant("badgeco", "Badge Co");
        (CookieClient browser, string key) = await OwnerAsync(tenant, "badgeco");
        using CookieClient owner = browser;

        // The Owner prints her own badge from her staff page.
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string member = Regex.Match(people, "href=\"(?<path>/staff/people/[0-9a-f-]{36})\"").Groups["path"].Value;
        (HttpResponseMessage _, string page) = await owner.GetAsync(member);
        Assert.Contains("No badge in use.", page, StringComparison.Ordinal);
        (HttpResponseMessage printed, string _) = await owner.PostAsync(member + "/badge", page);
        Assert.Equal("application/pdf", printed.Content.Headers.ContentType?.MediaType);
        await KnownBadgeAsync(tenant);
        (HttpResponseMessage _, string after) = await owner.GetAsync(member);
        Assert.Contains("is in use.", after, StringComparison.Ordinal);

        // Signed out on the licensed computer: the sign-in page offers the badge.
        (HttpResponseMessage _, string home) = await owner.GetAsync("/staff");
        await owner.PostAsync("/staff/sign-out", home);
        (HttpResponseMessage _, string signIn) = await owner.GetAsync("/staff");
        Assert.Contains("href=\"/staff/sign-in/badge\"", signIn, StringComparison.Ordinal);

        // First badge of the day: the authenticator code is asked.
        (HttpResponseMessage _, string badgePage) = await owner.GetAsync("/staff/sign-in/badge");
        (HttpResponseMessage wrong, string wrongPage) = await owner.PostAsync("/staff/sign-in/badge", badgePage, ("badge", "SB" + new string('Q', 25)));
        Assert.Null(wrong.Headers.Location);
        Assert.Contains("That badge or PIN did not work.", wrongPage, StringComparison.Ordinal);
        (HttpResponseMessage scanned, string _) = await owner.PostAsync("/staff/sign-in/badge", badgePage, ("badge", BadgeCode));
        Assert.StartsWith("/staff/sign-in/verify", scanned.Headers.Location?.OriginalString, StringComparison.Ordinal);
        (HttpResponseMessage _, string verify) = await owner.GetAsync(scanned.Headers.Location!.OriginalString);
        await owner.PostAsync("/staff/sign-in/verify", verify, ("code", Code(key, DateTime.UtcNow.AddSeconds(30))));
        Assert.NotNull(owner.Cookie(OperationsCookies.Session));

        // Later that day the badge alone signs in, and the device opens at the badge screen.
        (HttpResponseMessage _, string home2) = await owner.GetAsync("/staff");
        await owner.PostAsync("/staff/sign-out", home2);
        (HttpResponseMessage _, string quick) = await owner.GetAsync("/staff");
        Assert.Contains("action=\"/staff/sign-in/badge\"", quick, StringComparison.Ordinal);
        (HttpResponseMessage alone, string _) = await owner.PostAsync("/staff/sign-in/badge", quick, ("badge", BadgeCode));
        Assert.Equal("/staff", alone.Headers.Location?.OriginalString);
        Assert.NotNull(owner.Cookie(OperationsCookies.Session));

        // A badge session is never a fresh code: a sensitive action asks for it, and a PIN needs a full sign-in.
        (HttpResponseMessage _, string account) = await owner.GetAsync("/staff/account");
        (HttpResponseMessage asked, string _) = await owner.PostAsync("/staff/account/recovery-codes", account);
        Assert.StartsWith("/staff/confirm", asked.Headers.Location?.OriginalString, StringComparison.Ordinal);
        (HttpResponseMessage _, string pinRefused) = await owner.PostAsync("/staff/account/pin", account, ("currentPassword", Password), ("pin", "2580"));
        Assert.Contains("To set a PIN, sign in with your email and password first.", pinRefused, StringComparison.Ordinal);

        // Away from the licensed device, the badge opens nothing.
        using CookieClient elsewhere = _host.Browser("badgeco.techcrafters.ltd");
        (HttpResponseMessage _, string other) = await elsewhere.GetAsync("/staff/sign-in/badge");
        (HttpResponseMessage _, string refused) = await elsewhere.PostAsync("/staff/sign-in/badge", other, ("badge", BadgeCode));
        Assert.Contains("A badge or PIN works only on a device registered by the company.", refused, StringComparison.Ordinal);
        Assert.Null(elsewhere.Cookie(OperationsCookies.Session));
    }

    // The badge printed last gets a code the test knows (only its hash is kept).
    private async Task KnownBadgeAsync(Guid tenant)
    {
        await using SqlConnection connection = new(_database.ConnectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new("UPDATE ops.StaffBadges SET CodeHash = @hash WHERE TenantId = @tenant AND RevokedUtc IS NULL", connection);
        command.Parameters.AddWithValue("@hash", Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(BadgeCode))));
        command.Parameters.AddWithValue("@tenant", tenant);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    private static string Code(string manualKey, DateTime at) => new Totp(Base32Encoding.ToBytes(manualKey)).ComputeTotp(at);

    private Guid NewTenant(string slug, string company)
    {
        Guid tenant = Guid.NewGuid();
        _host.Platform.Tenant(slug, tenant, "active");
        _host.Platform.Accounts[tenant] = new OperationsTenantAccount(tenant, slug, company + " LLC", company, "en", "Grace Hopper", "grace@" + slug + ".example");
        return tenant;
    }

    // The Owner accepts the invitation, enrols an authenticator app and a computer.
    private async Task<(CookieClient Browser, string Key)> OwnerAsync(Guid tenant, string slug)
    {
        await _host.InviteOwnerAsync(tenant);
        CookieClient browser = _host.Browser(slug + ".techcrafters.ltd");
        string link = _host.Mailer.Invitations.Last(invitation => invitation.Link.Contains(slug, StringComparison.Ordinal)).Link;
        (HttpResponseMessage _, string invitation) = await browser.GetAsync(new Uri(link).PathAndQuery);
        (HttpResponseMessage accepted, string _) = await browser.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        (HttpResponseMessage _, string enrol) = await browser.GetAsync(accepted.Headers.Location!.OriginalString);
        string key = ManualKey.Match(enrol).Groups["key"].Value;
        await browser.PostAsync("/staff/sign-in/enrol", enrol, ("code", Code(key, DateTime.UtcNow)));
        (HttpResponseMessage _, string page) = await browser.GetAsync("/devices/enrol");
        await browser.PostAsync("/devices/enrol", page, ("name", "Handheld"), ("kind", "pc"));
        Assert.NotNull(browser.Cookie(OperationsCookies.Device));
        return (browser, key);
    }
}
