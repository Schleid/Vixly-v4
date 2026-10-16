using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Queries;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformCollectionsDatabaseTests.cs
//
// PURPOSE:
// Proves the Slice 6 mapping against SQL Server: settings, cases, history, the blocked subscription and the read models.
//
// RESPONSIBILITIES:
// - Round-trip settings, a case with its history and a blocked subscription
// - One open case per tenant and one card retry per invoice and day, even from another host
// - The clock's tenant list, the case list, overdue invoices and the dashboard
//
// IMPORTANT NOTES:
// - Needs VIXLY_TEST_SQL_SERVER; skipped otherwise
// =========================================================
public sealed class PlatformCollectionsDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Later = Now.AddDays(20);

    private readonly PlatformDatabaseFixture _database;

    public PlatformCollectionsDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task CaseHistoryAndBlockedSubscription_RoundTrip_AndTheReadModelsFindThem()
    {
        (PlatformTenant tenant, PlatformSubscription subscription, PlatformInvoice invoice) = NewOverdue();
        PlatformCollectionsSettings settings = PlatformCollectionsSettings.Create(Now);
        (PlatformCollectionCase collection, PlatformCollectionCaseEvent opened) = PlatformCollectionCase.Open(tenant.Id, subscription.Id, invoice.DueUtc, Now.AddDays(1));
        PlatformCollectionCaseEvent warning = collection.Advance(PlatformCollectionStep.FinalWarning, settings, Now.AddDays(10));
        PlatformCollectionCaseEvent blocked = collection.Advance(PlatformCollectionStep.Block, settings, Now.AddDays(14));
        subscription.BlockForNonPayment(invoice.Number, Now.AddDays(14));

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(tenant);
            context.Plans.AddRange(_plans);
            context.Subscriptions.Add(subscription);
            context.Invoices.Add(invoice);
            PlatformCollectionsRepository repository = new(context);

            if (await repository.FindSettingsAsync(CancellationToken.None) is null)
            {
                repository.AddSettings(settings);
            }

            repository.AddCase(collection);
            repository.AddEvents(new[] { opened, warning, blocked });
            Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        await using PlatformDbContext check = _database.NewContext();
        PlatformCollectionsRepository store = new(check);

        Assert.NotNull(await store.FindSettingsAsync(CancellationToken.None));
        PlatformCollectionCase loaded = (await store.GetOpenCaseAsync(tenant.Id, CancellationToken.None))!;
        Assert.Equal(PlatformCollectionStage.Blocked, loaded.Stage);
        Assert.Equal(collection.PlannedBlockUtc, loaded.PlannedBlockUtc);
        PlatformSubscription reloaded = (await new PlatformSubscriptionRepository(check).GetAsync(subscription.Id, CancellationToken.None))!;
        Assert.Equal(PlatformSubscriptionStatus.BlockedForNonPayment, reloaded.Status);
        Assert.Equal(PlatformSubscriptionStatus.Active, reloaded.StatusBeforeBlock);
        Assert.Contains(await store.ListTenantsToCheckAsync(Later, null, 5000, CancellationToken.None), id => id == tenant.Id);
        Assert.Single(await new PlatformInvoiceRepository(check).ListOverdueForTenantAsync(tenant.Id, Later, CancellationToken.None));

        PlatformCollectionsQueries queries = new(check);
        PagedResult<PlatformCollectionCaseItem> listed = await queries.ListCasesAsync(
            new PlatformCollectionCaseFilter(PlatformCollectionCaseStatus.Open, PlatformCollectionStage.Blocked, "USD", false, "Atlas", tenant.Id), PagedRequest.Create(1, 25), Later, CancellationToken.None);
        PlatformCollectionCaseItem row = Assert.Single(listed.Items);
        Assert.Equal(PlatformSubscriptionStatus.BlockedForNonPayment, row.SubscriptionStatus);
        Assert.Equal(new PlatformMoneyItem("USD", 99m), Assert.Single(row.Overdue));
        Assert.Equal(new[] { PlatformCollectionEventCodes.Blocked, PlatformCollectionEventCodes.FinalWarning, PlatformCollectionEventCodes.Opened },
            (await queries.ListEventsAsync(collection.Id, CancellationToken.None)).Select(item => item.EventCode));
        Assert.Single(await queries.ListOverdueInvoicesAsync(tenant.Id, Later, CancellationToken.None));

        PlatformCollectionsDashboard dashboard = await queries.GetDashboardAsync(Later, 60, CancellationToken.None);
        Assert.True(dashboard.Blocked >= 1);
        Assert.True(dashboard.OpenByStage[PlatformCollectionStage.Blocked] >= 1);
        Assert.Contains(dashboard.Overdue, item => item.CurrencyCode == "USD" && item.Amount >= 99m);
        Assert.Contains(dashboard.Ageing, item => item.Bucket == 1 && item.CurrencyCode == "USD");
    }

    [SqlServerFact]
    public async Task SecondOpenCase_AndASecondRetryForTheSameDay_FailToSave_FromAnotherHost()
    {
        (PlatformTenant tenant, PlatformSubscription subscription, PlatformInvoice invoice) = NewOverdue();
        PlatformCollectionCase first = PlatformCollectionCase.Open(tenant.Id, subscription.Id, invoice.DueUtc, Now.AddDays(1)).Case;

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(tenant);
            context.Plans.AddRange(_plans);
            context.Subscriptions.Add(subscription);
            context.Invoices.Add(invoice);
            context.CollectionCases.Add(first);
            context.CollectionCaseEvents.Add(first.RecordRetry(invoice.Id, invoice.Number, 3, Now.AddDays(3)));
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext other = _database.NewContext())
        {
            other.CollectionCases.Add(PlatformCollectionCase.Open(tenant.Id, subscription.Id, invoice.DueUtc, Now.AddDays(1)).Case);
            Assert.False(await new PlatformUnitOfWork(other).SaveChangesAsync(CancellationToken.None));
        }

        await using PlatformDbContext third = _database.NewContext();
        PlatformCollectionCase loaded = (await new PlatformCollectionsRepository(third).GetOpenCaseAsync(tenant.Id, CancellationToken.None))!;
        Assert.True(await new PlatformCollectionsRepository(third).RetryRecordedAsync(PlatformCollectionCase.RetryKeyFor(invoice.Id, 3), CancellationToken.None));
        third.CollectionCaseEvents.Add(loaded.RecordRetry(invoice.Id, invoice.Number, 3, Now.AddDays(3)));
        Assert.False(await new PlatformUnitOfWork(third).SaveChangesAsync(CancellationToken.None));
    }

    private (PlatformTenant Tenant, PlatformSubscription Subscription, PlatformInvoice Invoice) NewOverdue()
    {
        Guid actor = _database.ActorUserId == Guid.Empty ? Guid.NewGuid() : _database.ActorUserId;
        PlatformTenant tenant = PlatformTenant.CreateByStaff(
            "TEN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            "col-" + Guid.NewGuid().ToString("N")[..8],
            PlatformCompanyDetails.Create("Atlas Freight LLC", "Atlas " + Guid.NewGuid().ToString("N")[..4], EmailAddress.Create("office@atlas.example"), PhoneNumber.Create("+1 305 555 0199"),
                PlatformPostalAddress.Create("1 Dock Road", null, "Miami", "FL", "33101", "US")),
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Ada Byron", EmailAddress.Create("owner@atlas.example"), null),
            LanguageCode.En,
            "America/New_York",
            actor,
            Now).Tenant;
        PlatformPlan plan = PlatformPlan.Create("C" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(), "Growth", null, "USD", actor, Now);
        plan.EditDraft(PlatformPlanVersionTerms.Create("USD", 10m, 100m, 0m, 2, null, 10, 2, Array.Empty<PlatformPlanFeature>()), Now);
        plan.PublishDraft(actor, Now);
        PlatformSubscription subscription = PlatformSubscription.Start(tenant.Id, plan, PlatformSubscriptionTerm.Monthly, 3, 10, false, null, Now).Subscription;
        subscription.RecordOfflinePayment("Wire", actor, Now);
        PlatformInvoice invoice = PlatformInvoice.Issue(tenant.Id, subscription.Id, PlatformInvoiceKind.Renewal, "INV-C-" + Guid.NewGuid().ToString("N")[..10], "test:" + Guid.NewGuid().ToString("N"), "USD", LanguageCode.En,
            PlatformInvoiceParty.Create("Atlas Freight LLC", "1 Dock Road", null, "billing@atlas.example"), 0, Now, Now.AddMonths(1),
            new[] { new PlatformInvoiceLineInput(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.charge", new Dictionary<string, string>(StringComparer.Ordinal), 1m, 99m, 0m, null) },
            Now).Invoice;

        _plans.Add(plan);
        return (tenant, subscription, invoice);
    }

    private readonly List<PlatformPlan> _plans = new();
}
