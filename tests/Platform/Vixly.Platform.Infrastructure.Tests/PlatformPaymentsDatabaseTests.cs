using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Payments;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Queries;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformPaymentsDatabaseTests.cs
//
// PURPOSE:
// Proves the Slice 5 mapping and guarantees against SQL Server, and the provider adapters' safety checks.
//
// RESPONSIBILITIES:
// - Merchant account, profile, cards, transactions, links and events round-trip
// - One open charge per invoice and one row per provider event, enforced by SQL Server
// - The clock's lists; Stripe key and mode checks; simulated webhook signatures
//
// IMPORTANT NOTES:
// - Database tests run only when VIXLY_TEST_SQL_SERVER is set (SqlServerFact)
// =========================================================
public sealed class PlatformPaymentsDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PlatformDatabaseFixture _database;

    public PlatformPaymentsDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task CardRecords_RoundTrip_AndTheClockListsFindTheirWork()
    {
        (PlatformTenant tenant, PlatformInvoice invoice) = NewInvoice();
        PlatformTenantCardProfile profile = PlatformTenantCardProfile.Create(tenant.Id, PlatformCardProvider.Stripe, PlatformPaymentMode.Test, "cus_" + Suffix(), Now);
        profile.GiveConsent("owner@atlas.example", "2026-10", Now);
        PlatformSavedCard card = PlatformSavedCard.Create(tenant.Id, PlatformCardProvider.Stripe, "pm_" + Suffix(), "visa", "4242", 12, 2030, PlatformCardAddedVia.SaveCardLink, Now);
        PlatformCardTransaction charge = PlatformCardTransaction.Charge(tenant.Id, invoice.Id, PlatformCardTransactionOrigin.PayLink, PlatformCardProvider.Stripe, invoice.Balance, "USD", null, null, Now);
        charge.AttachCheckout("cs_" + Suffix(), Now);
        PlatformPaymentLink link = PlatformPaymentLink.Create(PlatformPaymentLinkKind.PayInvoice, tenant.Id, invoice.Id, Hash(), null, Now);

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(tenant);
            context.Invoices.Add(invoice);
            PlatformPaymentsRepository repository = new(context);
            repository.AddProfile(profile);
            repository.AddCard(card);
            repository.AddTransaction(charge);
            repository.AddLink(link);
            Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        await using PlatformDbContext check = _database.NewContext();
        PlatformPaymentsRepository payments = new(check);

        Assert.True((await payments.GetProfileAsync(tenant.Id, CancellationToken.None))!.AutoCharge);
        Assert.Equal("4242", Assert.Single(await payments.ListCardsAsync(tenant.Id, CancellationToken.None)).Last4);
        Assert.Equal(charge.Id, (await payments.GetOpenChargeAsync(invoice.Id, CancellationToken.None))!.Id);
        Assert.Equal(charge.Id, (await payments.FindTransactionAsync(charge.ProviderCheckoutId, null, null, CancellationToken.None))!.Id);
        Assert.Equal(link.Id, (await payments.GetLinkAsync(link.TokenHash, CancellationToken.None))!.Id);

        PlatformPaymentsQueries queries = new(check);
        Assert.Contains(await queries.ListAutoChargeInvoicesAsync(Now.AddDays(1), 500, CancellationToken.None), id => id == invoice.Id);
        Assert.Contains(await queries.ListStaleTransactionsAsync(Now.AddHours(1), 500, CancellationToken.None), id => id == charge.Id);
        PagedResult<PlatformCardTransactionItem> listed = await queries.ListTransactionsAsync(new PlatformCardTransactionFilter(null, null, tenant.Id, null, false), PagedRequest.Create(1, 25), CancellationToken.None);
        PlatformCardTransactionItem row = Assert.Single(listed.Items);
        Assert.Equal(invoice.Number, row.InvoiceNumber);
        Assert.Equal("Atlas", row.TenantName);
    }

    [SqlServerFact]
    public async Task SecondOpenChargeForAnInvoice_FailsToSave_EvenFromAnotherHost()
    {
        (PlatformTenant tenant, PlatformInvoice invoice) = NewInvoice();

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(tenant);
            context.Invoices.Add(invoice);
            context.CardTransactions.Add(PlatformCardTransaction.Charge(tenant.Id, invoice.Id, PlatformCardTransactionOrigin.StaffCharge, PlatformCardProvider.Stripe, 10m, "USD", null, null, Now));
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext other = _database.NewContext();
        other.CardTransactions.Add(PlatformCardTransaction.Charge(tenant.Id, invoice.Id, PlatformCardTransactionOrigin.Automatic, PlatformCardProvider.Stripe, 10m, "USD", null, null, Now));

        Assert.False(await new PlatformUnitOfWork(other).SaveChangesAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task SameProviderEvent_IsStoredOnce()
    {
        string eventId = "evt_" + Suffix();

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.ProviderEvents.Add(PlatformProviderEvent.Receive(PlatformCardProvider.Stripe, eventId, "payment_intent.succeeded", "pi_1", Now));
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext again = _database.NewContext();
        Assert.True(await new PlatformPaymentsRepository(again).EventSeenAsync(PlatformCardProvider.Stripe, eventId, CancellationToken.None));
        again.ProviderEvents.Add(PlatformProviderEvent.Receive(PlatformCardProvider.Stripe, eventId, "payment_intent.succeeded", "pi_1", Now));
        Assert.False(await new PlatformUnitOfWork(again).SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public void StripeKeys_AreRequired_AndMustMatchTheEnvironment()
    {
        Assert.Equal("platform.payments.secret_missing", PlatformCardModes.Stripe(new PlatformStripeOptions(), "Production").ProblemCode);
        Assert.Equal("platform.payments.mode_mismatch", PlatformCardModes.Stripe(new PlatformStripeOptions { SecretKey = "sk_test_x", WebhookSecret = "whsec_x" }, "Production").ProblemCode);
        Assert.Equal("platform.payments.mode_mismatch", PlatformCardModes.Stripe(new PlatformStripeOptions { SecretKey = "sk_live_x", WebhookSecret = "whsec_x" }, "Development").ProblemCode);

        PlatformCardGatewayStatus live = PlatformCardModes.Stripe(new PlatformStripeOptions { SecretKey = "sk_live_x", WebhookSecret = "whsec_x" }, "Production");
        Assert.True(live.Configured);
        Assert.Equal(PlatformPaymentMode.Live, live.Mode);
        Assert.DoesNotContain("sk_", new PlatformStripeOptions { SecretKey = "sk_live_secret" }.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void StripeWebhook_WithAWrongSignature_IsRefused()
    {
        StripeCardGateway gateway = new(Options(new PlatformPaymentsOptions { Stripe = new PlatformStripeOptions { SecretKey = "sk_test_x", WebhookSecret = "whsec_test" } }), "Development", NullLogger<StripeCardGateway>.Instance);

        Vixly.Shared.Kernel.Domain.DomainException refused = Assert.Throws<Vixly.Shared.Kernel.Domain.DomainException>(
            () => gateway.ParseEvent("{\"id\":\"evt_1\",\"object\":\"event\",\"type\":\"payment_intent.succeeded\"}", "t=1,v1=forged"));
        Assert.Equal("platform.payments.event_invalid", refused.Error.Code);
    }

    [Fact]
    public void SimulatedProvider_WorksOnlyInDevelopment_AndChecksItsSignature()
    {
        Assert.False(new SimulatedCardGateway(Options(new PlatformPaymentsOptions()), "Production", new NoHttp(), NullLogger<SimulatedCardGateway>.Instance).Status.Configured);

        SimulatedCardGateway gateway = new(Options(new PlatformPaymentsOptions()), "Development", new NoHttp(), NullLogger<SimulatedCardGateway>.Instance);
        Assert.True(gateway.Status.Configured);
        Assert.Equal("platform.payments.event_invalid",
            Assert.Throws<Vixly.Shared.Kernel.Domain.DomainException>(() => gateway.ParseEvent("{}", "00")).Error.Code);
    }

    private static IOptions<PlatformPaymentsOptions> Options(PlatformPaymentsOptions value) => Microsoft.Extensions.Options.Options.Create(value);

    private (PlatformTenant Tenant, PlatformInvoice Invoice) NewInvoice()
    {
        PlatformTenant tenant = PlatformTenant.CreateByStaff(
            "TEN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            "pay-" + Guid.NewGuid().ToString("N")[..8],
            PlatformCompanyDetails.Create("Atlas Freight LLC", "Atlas", EmailAddress.Create("office@atlas.example"), PhoneNumber.Create("+1 305 555 0199"),
                PlatformPostalAddress.Create("1 Dock Road", null, "Miami", "FL", "33101", "US")),
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Ada Byron", EmailAddress.Create("owner@atlas.example"), null),
            LanguageCode.En,
            "America/New_York",
            _database.ActorUserId == Guid.Empty ? Guid.NewGuid() : _database.ActorUserId,
            Now).Tenant;
        PlatformInvoice invoice = PlatformInvoice.Issue(tenant.Id, null, PlatformInvoiceKind.Renewal, "INV-P-" + Suffix(), "test:" + Guid.NewGuid().ToString("N"), "USD", LanguageCode.En,
            PlatformInvoiceParty.Create("Atlas Freight LLC", "1 Dock Road", null, "billing@atlas.example"), 0, Now, Now.AddMonths(1),
            new[] { new PlatformInvoiceLineInput(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.charge", new Dictionary<string, string>(StringComparer.Ordinal), 1m, 99m, 0m, null) },
            Now).Invoice;
        return (tenant, invoice);
    }

    private static string Hash() => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Guid.NewGuid().ToByteArray()));

    private static string Suffix() => Guid.NewGuid().ToString("N")[..10];

    private sealed class NoHttp : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("No HTTP in tests.");
    }
}
