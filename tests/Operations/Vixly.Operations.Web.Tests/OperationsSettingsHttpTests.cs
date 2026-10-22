using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using OtpNet;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsSettingsHttpTests.cs
//
// PURPOSE:
// Proves settings, company, billing, the payment gate and the audit log end to end (Slice 1E).
//
// RESPONSIBILITIES:
// - Settings are saved and the setup checklist moves; the audit log shows the change
// - Company contacts and a change request with proof go to Platform with the step-up
// - Billing shows the invoice, sends to the pay link and serves the PDF; only the Owner and Administrators see it
// - While a payment is due, the Owner lands on billing and other staff on the limited page
//
// IMPORTANT NOTES:
// - Real pipeline, real database (VIXLY_TEST_SQL_SERVER); Platform and mail are stood in
// =========================================================
public sealed class OperationsSettingsHttpTests : IClassFixture<OperationsWebDatabase>, IAsyncLifetime
{
    private const string Password = "harbour lights at dawn";

    private static readonly Regex ManualKey = new("<code>(?<key>[A-Z2-7]{16,})</code>", RegexOptions.CultureInvariant);

    private readonly OperationsWebDatabase _database;

    private OperationsWebTestHost _host = null!;

    public OperationsSettingsHttpTests(OperationsWebDatabase database)
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
    public async Task Settings_AreSaved_TheChecklistMoves_AndTheHistoryShowsIt()
    {
        NewTenant("setco", "Set Co");
        using CookieClient owner = await OwnerAsync("setco");

        (HttpResponseMessage _, string home) = await owner.GetAsync("/staff");
        Assert.Contains("<a href=\"/staff/settings\">Choose the currency and units</a>", home, StringComparison.Ordinal);

        (HttpResponseMessage _, string page) = await owner.GetAsync("/staff/settings");
        (HttpResponseMessage _, string same) = await owner.PostAsync("/staff/settings", page, Fields("A", "A"));
        Assert.Contains("Each prefix must be different.", same, StringComparison.Ordinal);
        (HttpResponseMessage saved, string _) = await owner.PostAsync("/staff/settings", page, Fields("ENV", "RC"));
        Assert.Equal("/staff/settings?done=1", saved.Headers.Location?.OriginalString);

        (HttpResponseMessage _, string after) = await owner.GetAsync("/staff/settings");
        Assert.Matches("<option value=\"HTG\" selected", after);
        Assert.Matches("<option value=\"Pound\" selected", after);
        Assert.Contains("value=\"ENV\"", after, StringComparison.Ordinal);
        (HttpResponseMessage _, string done) = await owner.GetAsync("/staff");
        Assert.Contains("✓ Choose the currency and units", done, StringComparison.Ordinal);

        (HttpResponseMessage _, string history) = await owner.GetAsync("/staff/audit?area=settings");
        Assert.Contains("ops.settings.changed", history, StringComparison.Ordinal);
        Assert.Contains("Grace Hopper", history, StringComparison.Ordinal);
        (HttpResponseMessage _, string roles) = await owner.GetAsync("/staff/audit?area=roles");
        Assert.DoesNotContain("ops.settings.changed", roles, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task ShippingServices_ChosenByTheTenant_AreSavedAndShown()
    {
        NewTenant("shipco", "Ship Co");
        using CookieClient owner = await OwnerAsync("shipco");

        (HttpResponseMessage _, string page) = await owner.GetAsync("/staff/services");
        Assert.Contains("Ocean full container", page, StringComparison.Ordinal);
        Assert.Contains("Priced by container", page, StringComparison.Ordinal);
        Assert.Contains("value=\"6000\"", page, StringComparison.Ordinal);

        (HttpResponseMessage saved, string _) = await owner.PostAsync("/staff/services", page,
            ("offered", "air_standard"), ("offered", "ground"),
            ("label_air_standard", "Air Miami"), ("min_air_standard", "3"), ("max_air_standard", "5"), ("divisor_air_standard", "5000"),
            ("min_air_express", "2"), ("max_air_express", "3"), ("divisor_air_express", "6000"),
            ("min_ocean_consolidated", "14"), ("max_ocean_consolidated", "28"), ("divisor_ocean_consolidated", "1000"),
            ("min_ocean_full_container", "14"), ("max_ocean_full_container", "35"),
            ("min_ground", "1"), ("max_ground", "2"), ("divisor_ground", "5000"));
        Assert.Equal("/staff/services?done=1", saved.Headers.Location?.OriginalString);

        (HttpResponseMessage _, string after) = await owner.GetAsync("/staff/services");
        Assert.Contains("value=\"Air Miami\"", after, StringComparison.Ordinal);
        Assert.Matches("value=\"ground\" checked", after);
        Assert.DoesNotMatch("value=\"air_express\" checked", after);
        (HttpResponseMessage _, string home) = await owner.GetAsync("/staff");
        Assert.Contains("✓ Choose your shipping services", home, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task Company_ContactsAndChangeRequest_GoToPlatform_WithStepUp()
    {
        NewTenant("compco", "Comp Co");
        using CookieClient owner = await OwnerAsync("compco");

        (HttpResponseMessage _, string page) = await owner.GetAsync("/staff/company");
        Assert.Contains("Acme Freight SA", page, StringComparison.Ordinal);
        (HttpResponseMessage saved, string _) = await owner.PostAsync("/staff/company/contacts", page,
            ("billingName", "Ada Lovelace"), ("billingEmail", "ada@compco.example"), ("billingPhone", "+50922224444"), ("language", "fr"), ("timeZone", "America/Port-au-Prince"));
        Assert.Equal("/staff/company?done=saved", saved.Headers.Location?.OriginalString);
        var update = Assert.Single(_host.Company.Updates);
        Assert.True(update.Actor.StepUp);
        Assert.Equal("owner", update.Actor.Role);
        Assert.Equal("billing", Assert.Single(update.Contacts).Role);
        Assert.Equal("fr", update.Language);

        (HttpResponseMessage sent, string _) = await owner.PostFormWithFileAsync("/staff/company/change", page, "proof", "proof.pdf", "application/pdf", Encoding.ASCII.GetBytes("%PDF-1.7 proof"),
            ("legalName", "Acme Freight Haiti SA"), ("email", "billing@acme.example"), ("phone", "+50922223333"), ("line1", "12 Rue Capois"), ("city", "Port-au-Prince"),
            ("country", "ht"), ("reason", "New legal name"));
        Assert.Equal("/staff/company?done=change", sent.Headers.Location?.OriginalString);
        var change = Assert.Single(_host.Company.Changes);
        Assert.Equal("proof.pdf", change.Input.ProofFileName);
        Assert.Equal("HT", change.Input.CountryCode);

        (HttpResponseMessage _, string waiting) = await owner.GetAsync("/staff/company");
        Assert.Contains("Waiting for support", waiting, StringComparison.Ordinal);
        Assert.Contains("/staff/company/change/withdraw", waiting, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task Billing_ShowsTheInvoice_SendsToThePayLink_AndServesThePdf_ForTheOwnerOnly()
    {
        NewTenant("billco", "Bill Co");
        using CookieClient owner = await OwnerAsync("billco");

        (HttpResponseMessage _, string page) = await owner.GetAsync("/staff/billing");
        Assert.Contains("VX-2026-0042", page, StringComparison.Ordinal);
        Assert.Contains("Payment late", page, StringComparison.Ordinal);
        (HttpResponseMessage pay, string _) = await owner.PostAsync($"/staff/billing/invoices/{FakeCompanyClient.InvoiceId}/pay", page);
        Assert.StartsWith("https://pay.example/link/", pay.Headers.Location?.OriginalString, StringComparison.Ordinal);
        (HttpResponseMessage pdf, string _) = await owner.GetAsync($"/staff/billing/documents/invoice/{FakeCompanyClient.DocumentId}");
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);

        using CookieClient ada = await StaffAsync(owner, "billco");
        (HttpResponseMessage _, string refused) = await ada.GetAsync("/staff/billing");
        Assert.Contains("Only the Owner and Administrators see billing.", refused, StringComparison.Ordinal);
        Assert.DoesNotContain("VX-2026-0042", refused, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task PaymentDue_SendsTheOwnerToBilling_AndOtherStaffToTheLimitedPage()
    {
        Guid tenant = NewTenant("dueco", "Due Co");
        using CookieClient owner = await OwnerAsync("dueco");
        using CookieClient ada = await StaffAsync(owner, "dueco");

        _host.Platform.Tenant("dueco", tenant, "active", access: "payment_required");
        await _host.ForgetTenantAsync(tenant);

        (HttpResponseMessage toBilling, string _) = await owner.GetAsync("/staff/people");
        Assert.Equal("/staff/billing", toBilling.Headers.Location?.OriginalString);
        (HttpResponseMessage billing, string page) = await owner.GetAsync("/staff/billing");
        Assert.Equal(HttpStatusCode.OK, billing.StatusCode);
        Assert.Contains("Open billing", page, StringComparison.Ordinal);
        (HttpResponseMessage pay, string _) = await owner.PostAsync($"/staff/billing/invoices/{FakeCompanyClient.InvoiceId}/pay", page);
        Assert.StartsWith("https://pay.example/", pay.Headers.Location?.OriginalString, StringComparison.Ordinal);

        (HttpResponseMessage limited, string _) = await ada.GetAsync("/staff/site");
        Assert.Equal("/staff/limited", limited.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string why) = await ada.GetAsync("/staff/limited");
        Assert.Contains("The Owner or an Administrator can pay from the billing page.", why, StringComparison.Ordinal);
    }

    private static (string, string)[] Fields(string shipment, string receiving) => new[]
    {
        ("currencyCode", "HTG"), ("weightUnit", "Pound"), ("dimensionUnit", "Inch"), ("allowCollect", "true"), ("pickup", "true"), ("homeDelivery", "true"),
        ("shipmentPrefix", shipment), ("receivingPrefix", receiving), ("invoicePrefix", "INV"), ("paymentPrefix", "PAY")
    };

    private static string Code(string manualKey, DateTime at) => new Totp(Base32Encoding.ToBytes(manualKey)).ComputeTotp(at);

    private Guid NewTenant(string slug, string company)
    {
        Guid tenant = Guid.NewGuid();
        _host.Platform.Tenant(slug, tenant, "active");
        _host.Platform.Accounts[tenant] = new OperationsTenantAccount(tenant, slug, company + " LLC", company, "en", "Grace Hopper", "grace@" + slug + ".example");
        return tenant;
    }

    private static async Task EnrolAsync(CookieClient browser)
    {
        (HttpResponseMessage _, string page) = await browser.GetAsync("/devices/enrol");
        await browser.PostAsync("/devices/enrol", page, ("name", "Laptop"), ("kind", "pc"));
        Assert.NotNull(browser.Cookie(OperationsCookies.Device));
    }

    // Ada, invited by the Owner with a role that edits the website only.
    private async Task<CookieClient> StaffAsync(CookieClient owner, string slug)
    {
        (HttpResponseMessage _, string form) = await owner.GetAsync("/staff/roles/new");
        await owner.PostAsync("/staff/roles/save", form, ("name", "Website editor"), ("permissions", "ops.brand.view"), ("permissions", "ops.brand.edit"));
        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        string role = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">Website editor</option>").Groups["id"].Value;
        await owner.PostAsync("/staff/people/invite", people, ("fullName", "Ada Lovelace"), ("email", "ada@" + slug + ".example"), ("language", "en"), ("roleId", role), ("scope", "tenant"));

        string link = _host.Mailer.Invitations.Last().Link;
        CookieClient ada = _host.Browser(slug + ".techcrafters.ltd");
        (HttpResponseMessage _, string invitation) = await ada.GetAsync(new Uri(link).PathAndQuery);
        await ada.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        await EnrolAsync(ada);
        return ada;
    }

    private async Task<CookieClient> OwnerAsync(string slug)
    {
        Guid tenant = _host.Platform.Portals[slug].TenantId!.Value;
        await _host.InviteOwnerAsync(tenant);
        CookieClient browser = _host.Browser(slug + ".techcrafters.ltd");
        string link = _host.Mailer.Invitations.Last(invitation => invitation.Link.Contains(slug, StringComparison.Ordinal)).Link;
        (HttpResponseMessage _, string invitation) = await browser.GetAsync(new Uri(link).PathAndQuery);
        (HttpResponseMessage accepted, string _) = await browser.PostAsync("/invitation", invitation, ("token", Uri.UnescapeDataString(link.Split("token=")[1])), ("password", Password));
        (HttpResponseMessage _, string enrol) = await browser.GetAsync(accepted.Headers.Location!.OriginalString);
        await browser.PostAsync("/staff/sign-in/enrol", enrol, ("code", Code(ManualKey.Match(enrol).Groups["key"].Value, DateTime.UtcNow)));
        Assert.NotNull(browser.Cookie(OperationsCookies.Session));
        await EnrolAsync(browser);
        return browser;
    }
}
