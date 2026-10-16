using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Queries;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformContractsDatabaseTests.cs
//
// PURPOSE:
// Proves the Slice 9 storage on SQL Server: the event feed sequence and its unique rules, and the tenant's billing document list.
//
// RESPONSIBILITIES:
// - Sequence grows in insert order; one row per outbox message and type
// - Pages read after a cursor, by type and settle time; old rows are removed
// - A tenant lists only its own billing documents
//
// IMPORTANT NOTES:
// - Runs only when VIXLY_TEST_SQL_SERVER is set
// =========================================================
public sealed class PlatformContractsDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly PlatformDatabaseFixture _database;

    public PlatformContractsDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task EventFeed_GrowsInOrder_KeepsOneRowPerMessageAndType_AndPrunesOldRows()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Guid tenant = Guid.NewGuid();
        PlatformPublishedEvent old = PlatformPublishedEvent.Create("platform.device.revoked.v1", tenant, Guid.NewGuid(), "{}", Now.AddDays(-40), Now.AddDays(-40));
        PlatformPublishedEvent a = PlatformPublishedEvent.Create("platform.device.approved.v1", tenant, first, "{\"n\":1}", Now, Now);
        PlatformPublishedEvent b = PlatformPublishedEvent.Create("platform.licensing.seats_changed.v1", tenant, first, "{\"n\":2}", Now, Now);
        PlatformPublishedEvent c = PlatformPublishedEvent.Create("platform.tenant.status_changed.v1", tenant, second, "{\"n\":3}", Now, Now);

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformPublishedEventRepository repository = new(context);
            repository.Add(old);
            repository.Add(a);
            repository.Add(b);
            Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        // A later commit always gets a later sequence.
        await using (PlatformDbContext later = _database.NewContext())
        {
            new PlatformPublishedEventRepository(later).Add(c);
            Assert.True(await new PlatformUnitOfWork(later).SaveChangesAsync(CancellationToken.None));
        }

        await using (PlatformDbContext again = _database.NewContext())
        {
            again.PublishedEvents.Add(PlatformPublishedEvent.Create("platform.device.approved.v1", tenant, first, "{}", Now, Now));
            Assert.False(await new PlatformUnitOfWork(again).SaveChangesAsync(CancellationToken.None));
        }

        await using PlatformDbContext check = _database.NewContext();
        PlatformPublishedEventRepository feed = new(check);
        Assert.True(await feed.ExistsAsync(first, "platform.licensing.seats_changed.v1", CancellationToken.None));
        Assert.False(await feed.ExistsAsync(second, "platform.licensing.seats_changed.v1", CancellationToken.None));

        string[] all = { "platform.device.approved.v1", "platform.licensing.seats_changed.v1", "platform.tenant.status_changed.v1", "platform.device.revoked.v1" };
        IReadOnlyList<PlatformPublishedEvent> page = (await feed.ListAfterAsync(0, 10_000, Now.AddSeconds(1), all, CancellationToken.None))
            .Where(item => item.PlatformTenantId == tenant).ToArray();
        // One save may insert its rows in any order; the feed is always in sequence order.
        Assert.Equal(new[] { "{}", "{\"n\":1}", "{\"n\":2}", "{\"n\":3}" }.Order(StringComparer.Ordinal), page.Select(item => item.Payload).Order(StringComparer.Ordinal));
        Assert.True(page.Zip(page.Skip(1)).All(pair => pair.First.Sequence < pair.Second.Sequence));

        long cursor = page.Where(item => item.Id != c.Id).Max(item => item.Sequence);
        IReadOnlyList<PlatformPublishedEvent> edge = (await feed.ListAfterAsync(cursor, 10_000, Now.AddSeconds(1), new[] { "platform.tenant.status_changed.v1" }, CancellationToken.None))
            .Where(item => item.PlatformTenantId == tenant).ToArray();
        Assert.Equal(c.Id, Assert.Single(edge).Id);
        Assert.Equal(c.Id, page[^1].Id);
        Assert.DoesNotContain(await feed.ListAfterAsync(cursor, 10_000, Now, all, CancellationToken.None), item => item.PlatformTenantId == tenant);

        Assert.True(await feed.DeleteCreatedBeforeAsync(Now.AddDays(-30), CancellationToken.None) >= 1);
        Assert.False(await check.PublishedEvents.AnyAsync(item => item.Id == old.Id));
        Assert.True(await check.PublishedEvents.AnyAsync(item => item.Id == a.Id));
    }

    [SqlServerFact]
    public async Task BillingDocuments_ListOnlyTheTenantsOwn_NewestFirst_WithoutTheirContent()
    {
        PlatformTenant tenant = NewTenant();
        PlatformTenant other = NewTenant();
        PlatformInvoice invoice = NewInvoice(tenant);
        PlatformInvoice foreign = NewInvoice(other);
        byte[] pdf = { 37, 80, 68, 70 };

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.AddRange(tenant, other);
            PlatformInvoiceRepository invoices = new(context);
            invoices.Add(invoice);
            invoices.Add(foreign);
            context.BillingDocuments.Add(PlatformBillingDocument.Create(PlatformBillingDocumentKind.Invoice, invoice.Id, invoice.Id, invoice.Number, LanguageCode.Create("fr"), pdf, Now));
            context.BillingDocuments.Add(PlatformBillingDocument.Create(PlatformBillingDocumentKind.Receipt, invoice.Id, Guid.NewGuid(), "RCT-" + invoice.Number, LanguageCode.Create("fr"), pdf, Now.AddMinutes(5)));
            context.BillingDocuments.Add(PlatformBillingDocument.Create(PlatformBillingDocumentKind.Invoice, foreign.Id, foreign.Id, foreign.Number, LanguageCode.Create("fr"), pdf, Now));
            Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        await using PlatformDbContext check = _database.NewContext();
        PagedResult<PlatformBillingDocumentItem> page = await new PlatformBillingQueries(check).ListDocumentsAsync(tenant.Id, PagedRequest.Create(1, 25), CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(new[] { PlatformBillingDocumentKind.Receipt, PlatformBillingDocumentKind.Invoice }, page.Items.Select(item => item.Kind));
        Assert.All(page.Items, item => Assert.Equal(invoice.Number, item.InvoiceNumber));
        Assert.Equal("fr", page.Items[0].Language);
    }

    private static PlatformInvoice NewInvoice(PlatformTenant tenant)
    {
        return PlatformInvoice.Issue(
            tenant.Id,
            null,
            PlatformInvoiceKind.Renewal,
            "INV-K-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
            "test:" + Guid.NewGuid().ToString("N"),
            "USD",
            LanguageCode.Create("fr"),
            PlatformInvoiceParty.Create("Atlas Freight LLC", "1 Dock Road", null, "billing@atlas.example"),
            7,
            Now,
            Now.AddMonths(1),
            new[] { new PlatformInvoiceLineInput(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.plan_period", new Dictionary<string, string>(StringComparer.Ordinal), 1m, 50m, 0m, null) },
            Now).Invoice;
    }

    private PlatformTenant NewTenant()
    {
        return PlatformTenant.CreateByStaff(
            "TEN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            "doc-" + Guid.NewGuid().ToString("N")[..8],
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
}
