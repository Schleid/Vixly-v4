using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Documents;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Queries;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Platform.Resources;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformBillingDatabaseTests.cs
//
// PURPOSE:
// Proves the Slice 4 mapping and guarantees against SQL Server, and the PDF renderer.
//
// RESPONSIBILITIES:
// - Invoices round-trip with lines, payments, credit notes and history
// - One invoice per source event; numbers never repeat under a race
// - Settings, profile, tax rates and stored documents round-trip
// - Invoice list, tenant summary and the credit-note approvals
// - Invoices, credit notes and receipts render as PDF in each language
//
// IMPORTANT NOTES:
// - Database tests run only when VIXLY_TEST_SQL_SERVER is set (SqlServerFact)
// =========================================================
public sealed class PlatformBillingDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PlatformDatabaseFixture _database;

    public PlatformBillingDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task Invoice_RoundTrips_WithPaymentsCreditsAndHistory()
    {
        PlatformTenant tenant = NewTenant();
        PlatformInvoiceIssue issue = NewInvoice(tenant, "INV-T-" + Suffix(), Now.AddDays(-30));
        PlatformInvoice invoice = issue.Invoice;
        PlatformInvoicePaymentResult paid = invoice.RecordPayment("RCT-T-" + Suffix(), 40m, PlatformPaymentMethod.BankTransfer, Now, "WIRE 9", "First half", _database.ActorUserId, Now);
        invoice.RequestCredit(10m, false, "Late delivery", _database.ActorUserId, Now);

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(tenant);
            PlatformInvoiceRepository repository = new(context);
            repository.Add(invoice);
            repository.AddEvents(new[] { issue.Event, paid.Event });
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext check = _database.NewContext();
        PlatformInvoiceRepository invoices = new(check);
        PlatformInvoice loaded = (await invoices.GetAsync(invoice.Id, CancellationToken.None))!;

        Assert.Equal(invoice.Total, loaded.Total);
        Assert.Equal(3, loaded.Lines.Count);
        Assert.Equal("Atlas", loaded.Lines.Single(line => line.Kind == PlatformInvoiceLineKind.PlanPeriod).Arguments["plan"]);
        Assert.Equal(40m, Assert.Single(loaded.Payments).Amount);
        Assert.NotNull(loaded.PendingCredit);
        Assert.Equal("Atlas Freight LLC", loaded.Customer.Name);
        Assert.Equal(LanguageCode.Create("fr"), loaded.Language);
        Assert.True(await invoices.SourceBilledAsync(invoice.SourceKey, CancellationToken.None));
        Assert.Contains(await invoices.ListOverdueUnmarkedAsync(Now, 100, CancellationToken.None), id => id == invoice.Id);
        Assert.Equal(2, (await invoices.ListAsync(invoice.Id, CancellationToken.None)).Count);

        PagedResult<PlatformInvoiceListItem> list = await new PlatformBillingQueries(check).ListInvoicesAsync(
            new PlatformInvoiceListFilter(null, true, tenant.Id, invoice.Number[..6], null, null, Now), PagedRequest.Create(1, 25), CancellationToken.None);
        PlatformInvoiceListItem row = Assert.Single(list.Items);
        Assert.True(row.Overdue);
        Assert.True(row.CreditPending);
        PlatformTenantBillingSummary summary = Assert.Single(await new PlatformBillingQueries(check).SummaryAsync(tenant.Id, Now, CancellationToken.None));
        Assert.Equal(invoice.Balance, summary.OpenBalance);

        IReadOnlyList<PlatformApprovalItem> approvals = await new PlatformSubscriptionQueries(check).ListApprovalsAsync(new[] { PlatformApprovalKind.CreditNote }, "en", CancellationToken.None);
        Assert.Contains(approvals, item => item.TargetId == invoice.Id && item.Kind == PlatformApprovalKind.CreditNote);
    }

    [SqlServerFact]
    public async Task SameSourceEvent_CannotBeBilledTwice()
    {
        PlatformTenant tenant = NewTenant();
        PlatformInvoice first = NewInvoice(tenant, "INV-A-" + Suffix(), Now).Invoice;

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(tenant);
            context.Invoices.Add(first);
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext again = _database.NewContext();
        PlatformInvoice second = PlatformInvoice.Issue(tenant.Id, null, PlatformInvoiceKind.Renewal, "INV-B-" + Suffix(), first.SourceKey, "USD", LanguageCode.En,
            PlatformInvoiceParty.Create("X", "Y", null, "x@y.example"), 0, null, null, new[] { Line(PlatformInvoiceLineKind.PlanPeriod, 1m, 10m) }, Now).Invoice;
        again.Invoices.Add(second);

        Assert.False(await new PlatformUnitOfWork(again).SaveChangesAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task NumberSequence_NeverHandsOutTheSameNumberTwice()
    {
        int year = 3000 + Random.Shared.Next(0, 5000);

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.NumberSequences.Add(PlatformNumberSequence.Start(PlatformBillingDocumentKind.Receipt, year, Now));
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext one = _database.NewContext();
        await using PlatformDbContext two = _database.NewContext();
        PlatformNumberSequence a = (await new PlatformBillingRepository(one).GetSequenceAsync(PlatformBillingDocumentKind.Receipt, year, CancellationToken.None))!;
        PlatformNumberSequence b = (await new PlatformBillingRepository(two).GetSequenceAsync(PlatformBillingDocumentKind.Receipt, year, CancellationToken.None))!;

        Assert.Equal(a.Take("RCT", Now), b.Take("RCT", Now));
        Assert.True(await new PlatformUnitOfWork(one).SaveChangesAsync(CancellationToken.None));
        Assert.False(await new PlatformUnitOfWork(two).SaveChangesAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task NewSequence_TakenTwiceInOneCommit_ContinuesInsteadOfStartingAgain()
    {
        int year = 3000 + Random.Shared.Next(0, 5000);

        await using PlatformDbContext context = _database.NewContext();
        PlatformBillingRepository repository = new(context);
        Assert.Null(await repository.GetSequenceAsync(PlatformBillingDocumentKind.CreditNote, year, CancellationToken.None));
        repository.AddSequence(PlatformNumberSequence.Start(PlatformBillingDocumentKind.CreditNote, year, Now));

        PlatformNumberSequence first = (await repository.GetSequenceAsync(PlatformBillingDocumentKind.CreditNote, year, CancellationToken.None))!;
        string one = first.Take("CN", Now);
        string two = (await repository.GetSequenceAsync(PlatformBillingDocumentKind.CreditNote, year, CancellationToken.None))!.Take("CN", Now);

        Assert.NotEqual(one, two);
        Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task SettingsProfileTaxRatesAndDocuments_RoundTrip()
    {
        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformBillingRepository repository = new(context);

            if (await repository.GetSettingsAsync(CancellationToken.None) is null)
            {
                PlatformBillingSettings settings = PlatformBillingSettings.Create(Now);
                settings.Update(14, "INV", "CN", "RCT", PlatformPaperSize.Letter, true,
                    new Dictionary<LanguageCode, (string?, string?)> { [LanguageCode.Create("fr")] = ("Virement bancaire", "Merci") }, _database.ActorUserId, Now);
                repository.AddSettings(settings);
            }

            if (await repository.GetProfileAsync(CancellationToken.None) is null)
            {
                repository.AddProfile(Profile());
            }

            repository.AddTaxRate(PlatformTaxRate.Create("CA", "QC", 14.975m, "TPS+TVQ", Now.AddYears(-1), _database.ActorUserId, Now));
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext check = _database.NewContext();
        PlatformBillingRepository billing = new(check);
        PlatformBillingSettings loaded = (await billing.GetSettingsAsync(CancellationToken.None))!;
        PlatformCompanyProfile profile = (await billing.GetProfileAsync(CancellationToken.None))!;

        Assert.NotNull(loaded.TextIn(LanguageCode.Create("fr")));
        Assert.False(string.IsNullOrEmpty(profile.Company.LegalName));
        Assert.Contains(await billing.ListTaxRatesAsync("CA", CancellationToken.None), rate => rate.RegionCode == "QC" && rate.Rate == 14.975m);
    }

    [Fact]
    public void Renderer_ProducesPdfs_ForInvoiceCreditNoteAndReceipt()
    {
        QuestPdfPlatformBillingDocuments documents = new(new KeyText(), Options.Create(new PlatformDocumentOptions { QuestPdfLicense = "Community" }));
        PlatformInvoice invoice = NewInvoice(NewTenant(), "INV-2026-000001", Now).Invoice;
        PlatformPayment payment = invoice.RecordPayment("RCT-2026-000001", 20m, PlatformPaymentMethod.Cheque, Now, "CHQ 1", null, Guid.NewGuid(), Now).Payment;
        invoice.RequestCredit(5m, false, "Goodwill", Guid.NewGuid(), Now);
        PlatformCreditNote credit = invoice.DecideCredit(invoice.PendingCredit!.Id, true, "CN-2026-000001", null, Guid.NewGuid(), Now).Credit;
        PlatformCompanyProfile profile = Profile();
        PlatformBillingSettings settings = PlatformBillingSettings.Create(Now);

        foreach (byte[] pdf in new[]
        {
            documents.RenderInvoice(invoice, profile, settings),
            documents.RenderCreditNote(invoice, credit, profile, settings),
            documents.RenderReceipt(invoice, payment, profile, settings)
        })
        {
            Assert.True(pdf.Length > 1000);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        }
    }

    private PlatformCompanyProfile Profile()
    {
        return PlatformCompanyProfile.Create(
            PlatformCompanyDetails.Create("TechCrafters Inc.", "Vixly", EmailAddress.Create("billing@vixly.example"), PhoneNumber.Create("+1 305 555 0100"),
                PlatformPostalAddress.Create("100 Biscayne Blvd", null, "Miami", "FL", "33132", "US")),
            "US-99-1234567",
            "https://vixly.example",
            _database.ActorUserId == Guid.Empty ? Guid.NewGuid() : _database.ActorUserId,
            Now);
    }

    private static PlatformInvoiceIssue NewInvoice(PlatformTenant tenant, string number, DateTimeOffset issued)
    {
        return PlatformInvoice.Issue(
            tenant.Id,
            null,
            PlatformInvoiceKind.Renewal,
            number,
            "test:" + Guid.NewGuid().ToString("N"),
            "USD",
            LanguageCode.Create("fr"),
            PlatformInvoiceParty.Create("Atlas Freight LLC", "1 Dock Road\nMiami FL 33101\nUS", "US-123", "billing@atlas.example"),
            7,
            issued,
            issued.AddMonths(1),
            new[]
            {
                new PlatformInvoiceLineInput(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.plan_period_dates",
                    new Dictionary<string, string>(StringComparer.Ordinal) { ["plan"] = "Atlas", ["term"] = "Monthly", ["licences"] = "3", ["from"] = "2026-09-01", ["to"] = "2026-10-01" },
                    3m, 25m, 7.5m, "Sales tax"),
                new PlatformInvoiceLineInput(PlatformInvoiceLineKind.OneTimeCharge, "ui.invoice_line.charge",
                    new Dictionary<string, string>(StringComparer.Ordinal) { ["description"] = "Formation sur site" }, 1m, 250m, 7.5m, "Sales tax"),
                new PlatformInvoiceLineInput(PlatformInvoiceLineKind.Discount, "ui.invoice_line.discount", new Dictionary<string, string>(StringComparer.Ordinal), 1m, -15m, 7.5m, "Sales tax")
            },
            issued);
    }

    private static PlatformInvoiceLineInput Line(PlatformInvoiceLineKind kind, decimal quantity, decimal price)
    {
        return new PlatformInvoiceLineInput(kind, "ui.invoice_line.charge", new Dictionary<string, string>(StringComparer.Ordinal), quantity, price, 0m, null);
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    }

    private PlatformTenant NewTenant()
    {
        string slug = "bill-" + Guid.NewGuid().ToString("N")[..8];

        return PlatformTenant.CreateByStaff(
            "TEN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            slug,
            PlatformCompanyDetails.Create(
                "Atlas Freight LLC",
                "Atlas",
                EmailAddress.Create("office@atlas.example"),
                PhoneNumber.Create("+1 305 555 0199"),
                PlatformPostalAddress.Create("1 Dock Road", null, "Miami", "FL", "33101", "US")),
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Ada Byron", EmailAddress.Create("owner@atlas.example"), null),
            LanguageCode.Create("fr"),
            "America/New_York",
            _database.ActorUserId == Guid.Empty ? Guid.NewGuid() : _database.ActorUserId,
            Now).Tenant;
    }

    private sealed class KeyText : IStringLocalizer<PlatformText>
    {
        public LocalizedString this[string name] => new(name, name + " {number}{taxId}{plan}{term}{licences}{from}{to}{description}");

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
    }
}
