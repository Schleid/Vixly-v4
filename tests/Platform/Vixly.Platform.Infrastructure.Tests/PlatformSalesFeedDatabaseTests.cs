using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.SalesFeed;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.SalesFeed;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformSalesFeedDatabaseTests.cs
//
// PURPOSE:
// Proves the sales feed's storage rules on SQL Server (2026-11-02).
//
// RESPONSIBILITIES:
// - A sale and its history row are saved together; the row then waits for the feed
// - Once fed, the row is never offered again; a second event for it, a repeated sequence or a second totals row for a day is refused
// - Pending events come back by sequence; the state row round-trips
//
// IMPORTANT NOTES:
// - Shared test database: each test uses its own rows, sequences and days
// =========================================================
public sealed class PlatformSalesFeedDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly PlatformDatabaseFixture _database;

    public PlatformSalesFeedDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task SaleAndHistory_SavedTogether_FedOnce_ThenNeverOfferedAgain()
    {
        (PlatformTenant tenant, PlatformInvoiceIssue issue) = NewSale();

        await using (PlatformDbContext context = _database.NewContext())
        {
            // The sale and its history row in one save, as every sale is written.
            context.Tenants.Add(tenant);
            context.Invoices.Add(issue.Invoice);
            context.InvoiceEvents.Add(issue.Event);
            await context.SaveChangesAsync();
        }

        long sequence;

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformSalesFeedRepository feed = new(context);
            Assert.Contains(await feed.ListUnfedInvoiceEventsAsync(new[] { PlatformInvoiceEventCodes.Issued }, 100000, CancellationToken.None), row => row.Id == issue.Event.Id);
            sequence = await feed.LastSequenceAsync(CancellationToken.None) + 1;
            feed.Add(Event(sequence, PlatformSalesFeedSource.InvoiceEvent, issue.Event.Id, null, tenant.Id));
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformSalesFeedRepository feed = new(context);
            Assert.DoesNotContain(await feed.ListUnfedInvoiceEventsAsync(new[] { PlatformInvoiceEventCodes.Issued }, 100000, CancellationToken.None), row => row.Id == issue.Event.Id);
            Assert.Contains(await feed.ListPendingAsync(100000, CancellationToken.None), item => item.Sequence == sequence);
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.SalesFeedEvents.Add(Event(sequence + 1000000, PlatformSalesFeedSource.InvoiceEvent, issue.Event.Id, null, tenant.Id));
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.SalesFeedEvents.Add(Event(sequence, PlatformSalesFeedSource.InvoiceEvent, Guid.NewGuid(), null, tenant.Id));
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }

    [SqlServerFact]
    public async Task Totals_OneRowPerDay_DayItemsAndState()
    {
        DateOnly day = new(1990 + Random.Shared.Next(0, 30), 1 + Random.Shared.Next(0, 12), 1 + Random.Shared.Next(0, 28));
        long sequence;

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformSalesFeedRepository feed = new(context);
            sequence = await feed.LastSequenceAsync(CancellationToken.None) + 1;
            feed.Add(Event(sequence, PlatformSalesFeedSource.DailyTotals, null, day, null));
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.SalesFeedEvents.Add(Event(sequence + 1, PlatformSalesFeedSource.DailyTotals, null, day, null));
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformSalesFeedRepository feed = new(context);
            PlatformSalesFeedState state = await feed.GetStateAsync(CancellationToken.None) ?? Add(feed);
            state.Failed("http_503", Now);
            await context.SaveChangesAsync();
            Assert.True((await feed.LastTotalsDayAsync(CancellationToken.None)) >= day);
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformSalesFeedState state = (await new PlatformSalesFeedRepository(context).GetStateAsync(CancellationToken.None))!;
            Assert.Equal("http_503", state.LastFailure);
            state.Succeeded(Now);
            await context.SaveChangesAsync();
        }
    }

    private static PlatformSalesFeedState Add(PlatformSalesFeedRepository feed)
    {
        PlatformSalesFeedState state = PlatformSalesFeedState.Create(Now);
        feed.AddState(state);
        return state;
    }

    private static PlatformSalesFeedEvent Event(long sequence, PlatformSalesFeedSource source, Guid? sourceId, DateOnly? day, Guid? tenantId)
    {
        return PlatformSalesFeedEvent.Create(sequence, day is null ? PlatformSalesFeedTypes.InvoiceIssued : PlatformSalesFeedTypes.TotalsDaily, source, sourceId, day, tenantId, Now,
            day is null ? 99m : null, day is null ? "USD" : null, id => "{\"id\":\"" + id.ToString("D") + "\"}", Now);
    }

    private (PlatformTenant Tenant, PlatformInvoiceIssue Issue) NewSale()
    {
        PlatformTenant tenant = PlatformTenant.CreateByStaff(
            "TEN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            "feed-" + Guid.NewGuid().ToString("N")[..8],
            PlatformCompanyDetails.Create("Galeo Logistics LLC", "Galeo", EmailAddress.Create("office@galeo.example"), PhoneNumber.Create("+1 305 555 0199"),
                PlatformPostalAddress.Create("1 Dock Road", null, "Miami", "FL", "33101", "US")),
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Ada Byron", EmailAddress.Create("owner@galeo.example"), null),
            LanguageCode.En,
            "America/New_York",
            _database.ActorUserId == Guid.Empty ? Guid.NewGuid() : _database.ActorUserId,
            Now).Tenant;
        PlatformInvoiceIssue issue = PlatformInvoice.Issue(tenant.Id, null, PlatformInvoiceKind.Renewal, "INV-F-" + Guid.NewGuid().ToString("N")[..10], "test:" + Guid.NewGuid().ToString("N"), "USD", LanguageCode.En,
            PlatformInvoiceParty.Create("Galeo Logistics LLC", "1 Dock Road", null, "billing@galeo.example"), 0, Now, Now.AddMonths(1),
            new[] { new PlatformInvoiceLineInput(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.charge", new Dictionary<string, string>(StringComparer.Ordinal), 1m, 99m, 0m, null) },
            Now);
        return (tenant, issue);
    }
}
