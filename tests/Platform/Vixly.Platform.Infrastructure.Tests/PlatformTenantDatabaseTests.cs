using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
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
// FILE: PlatformTenantDatabaseTests.cs
//
// PURPOSE:
// Proves the Slice 2 database guarantees against a real SQL
// Server.
//
// RESPONSIBILITIES:
// - A slug can never be held twice, even by racing saves
// - Tenants round-trip with company, contacts, tags and history
// - A failed save leaves no outbox message (D-2, D-T17)
// - Leasing gives each message to one dispatcher, oldest first
//   per stream
// - Directory search finds by name, phone digits and tag
//
// IMPORTANT NOTES:
// - Skipped unless VIXLY_TEST_SQL_SERVER is set
// =========================================================
public sealed class PlatformTenantDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly PlatformDatabaseFixture _database;

    public PlatformTenantDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task TwoRacingClaims_ForOneSlug_OnlyOneCommits()
    {
        string slug = "race-" + Guid.NewGuid().ToString("N")[..8];
        await using PlatformDbContext first = _database.NewContext();
        await using PlatformDbContext second = _database.NewContext();
        first.PortalSlugs.Add(PlatformPortalSlug.ClaimForRegistration(slug, await RegistrationIdAsync(first), Now.AddHours(24), Now));
        second.PortalSlugs.Add(PlatformPortalSlug.ClaimForRegistration(slug, await RegistrationIdAsync(second), Now.AddHours(24), Now));

        bool firstSaved = await new PlatformUnitOfWork(first).SaveChangesAsync(CancellationToken.None);
        bool secondSaved = await new PlatformUnitOfWork(second).SaveChangesAsync(CancellationToken.None);

        Assert.True(firstSaved);
        Assert.False(secondSaved);
        await using PlatformDbContext check = _database.NewContext();
        Assert.Equal(1, await check.PortalSlugs.CountAsync(claim => claim.Slug == slug));
    }

    [SqlServerFact]
    public async Task Tenant_RoundTrips_WithCompanyContactsTagsAndHistory()
    {
        PlatformTenantCreation creation = NewTenant("round-trip");
        Guid tenantId = creation.Tenant.Id;

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(creation.Tenant);
            context.TenantLifecycleEvents.AddRange(creation.Events);
            creation.Tenant.SetTags(new[] { "enterprise", "pilot" }, Now);
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformTenant tenant = await context.Tenants.SingleAsync(item => item.Id == tenantId);
            // B12: legal details change only through an approved request.
            PlatformTenantChangeRequest change = PlatformTenantChangeRequest.Submit(
                tenant,
                PlatformCompanyDetails.Create(
                    "Northwind Logistics Ltd.",
                    null,
                    EmailAddress.Create("hello@northwind.example"),
                    PhoneNumber.Create("+1 514 555 0199"),
                    PlatformPostalAddress.Create("2 Dock Street", "Unit 4", "Laval", null, null, "CA")),
                "Merger.",
                "proof.pdf",
                "%PDF-1.7"u8.ToArray(),
                "ops-1",
                "Grace Hopper",
                Now);
            context.TenantChangeRequests.Add(change);
            context.TenantLifecycleEvents.Add(change.Approve(tenant, null, _database.ActorUserId, Now.AddSeconds(30)));
            PlatformTenantLifecycleEvent activated = tenant.Activate(Guid.NewGuid(), Now.AddMinutes(1));
            context.TenantLifecycleEvents.Add(activated);
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformTenant tenant = await context.Tenants.AsNoTracking().SingleAsync(item => item.Id == tenantId);
            Assert.Equal(("Northwind Logistics Ltd.", null, "Unit 4"), (tenant.Company.LegalName, tenant.Company.TradingName, tenant.Company.Address.Line2));
            Assert.Equal(PlatformTenantStatus.Active, tenant.Status);
            Assert.Equal(new[] { "enterprise", "pilot" }, tenant.Tags.Select(tag => tag.Value).Order(StringComparer.Ordinal));
            Assert.Equal("owner@northwind.example", tenant.ContactFor(PlatformTenantContactRole.Owner).Email.Value);

            PagedResult<PlatformTenantEventItem> history = await new PlatformTenantQueries(context)
                .ListEventsAsync(tenantId, PagedRequest.First, CancellationToken.None);
            Assert.Equal(
                new[] { PlatformTenantEventCodes.Activated, PlatformTenantEventCodes.CompanyChanged, PlatformTenantEventCodes.CreatedByStaff },
                history.Items.Select(item => item.EventCode));
        }
    }

    [SqlServerFact]
    public async Task FailedSave_LeavesNoOutboxMessage()
    {
        PlatformTenantCreation creation = NewTenant("stale");

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(creation.Tenant);
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext stale = _database.NewContext();
        PlatformTenant staleTenant = await stale.Tenants.SingleAsync(item => item.Id == creation.Tenant.Id);

        await using (PlatformDbContext winner = _database.NewContext())
        {
            PlatformTenant tenant = await winner.Tenants.SingleAsync(item => item.Id == creation.Tenant.Id);
            tenant.Activate(Guid.NewGuid(), Now);
            await winner.SaveChangesAsync();
        }

        staleTenant.Deactivate("Lead lost", Guid.NewGuid(), Now);
        stale.OutboxMessages.Add(PlatformOutboxMessage.Create("platform.tenant.status_changed.v1", staleTenant.Id, "{}", Now));

        Assert.False(await new PlatformUnitOfWork(stale).SaveChangesAsync(CancellationToken.None));
        await using PlatformDbContext check = _database.NewContext();
        Assert.False(await check.OutboxMessages.AnyAsync(message => message.StreamId == staleTenant.Id));
    }

    [SqlServerFact]
    public async Task Leasing_GivesEachMessageOnce_OldestFirstPerStream()
    {
        Guid streamA = Guid.NewGuid();
        Guid streamB = Guid.NewGuid();
        PlatformOutboxMessage a1 = PlatformOutboxMessage.Create("platform.test.v1", streamA, "{}", Now);
        PlatformOutboxMessage a2 = PlatformOutboxMessage.Create("platform.test.v1", streamA, "{}", Now.AddSeconds(1));
        PlatformOutboxMessage b1 = PlatformOutboxMessage.Create("platform.test.v1", streamB, "{}", Now.AddSeconds(2));

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.OutboxMessages.AddRange(a1, a2, b1);
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext one = _database.NewContext();
        await using PlatformDbContext two = _database.NewContext();
        DateTimeOffset later = Now.AddMinutes(1);
        Task<IReadOnlyList<Guid>> firstLease = new PlatformOutboxRepository(one).LeaseDueAsync(10, later, later.AddMinutes(2), CancellationToken.None);
        Task<IReadOnlyList<Guid>> secondLease = new PlatformOutboxRepository(two).LeaseDueAsync(10, later, later.AddMinutes(2), CancellationToken.None);
        IReadOnlyList<Guid>[] leases = await Task.WhenAll(firstLease, secondLease);
        Guid[] leased = leases.SelectMany(ids => ids).Where(id => id == a1.Id || id == a2.Id || id == b1.Id).ToArray();

        Assert.Equal(leased.Length, leased.Distinct().Count());
        Assert.Contains(a1.Id, leased);
        Assert.Contains(b1.Id, leased);
        Assert.DoesNotContain(a2.Id, leased);
    }

    [SqlServerFact]
    public async Task Directory_FindsByNamePhoneDigitsAndTag()
    {
        PlatformTenantCreation creation = NewTenant("search-" + Guid.NewGuid().ToString("N")[..6]);
        creation.Tenant.SetTags(new[] { "searchable" }, Now);

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(creation.Tenant);
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext read = _database.NewContext();
        PlatformTenantQueries queries = new(read);

        async Task<bool> FindsAsync(PlatformTenantListFilter filter)
        {
            PagedResult<PlatformTenantListItem> page = await queries.ListAsync(filter, PagedRequest.Create(1, 100), CancellationToken.None);
            return page.Items.Any(item => item.PlatformTenantId == creation.Tenant.Id);
        }

        Assert.True(await FindsAsync(new PlatformTenantListFilter("northwind freight", null, null, null)));
        Assert.True(await FindsAsync(new PlatformTenantListFilter("(514) 555-0100", null, null, null)));
        Assert.True(await FindsAsync(new PlatformTenantListFilter(null, PlatformTenantStatus.Draft, "searchable", null)));
        Assert.False(await FindsAsync(new PlatformTenantListFilter(null, PlatformTenantStatus.Active, null, null)));
        Assert.False(await FindsAsync(new PlatformTenantListFilter("100%_[", null, null, null)));
    }

    private PlatformTenantCreation NewTenant(string slug)
    {
        return PlatformTenant.CreateByStaff(
            "TEN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            slug,
            PlatformCompanyDetails.Create(
                "Northwind Freight Inc.",
                "Northwind",
                EmailAddress.Create("office@northwind.example"),
                PhoneNumber.Create("+1 514 555 0100"),
                PlatformPostalAddress.Create("1 Harbour Road", null, "Montreal", "QC", "H2X 1Y4", "CA")),
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Grace Hopper", EmailAddress.Create("owner@northwind.example"), null),
            LanguageCode.Fr,
            "America/Toronto",
            _database.ActorUserId,
            Now);
    }

    private async Task<Guid> RegistrationIdAsync(PlatformDbContext context)
    {
        PlatformTenantRegistration registration = PlatformTenantRegistration.Start(
            NewTenant("unused").Tenant.Company,
            "Grace Hopper",
            EmailAddress.Create("grace@northwind.example"),
            PhoneNumber.Create("+1 514 555 0101"),
            LanguageCode.En,
            null,
            "unused",
            Convert.ToHexStringLower(Guid.NewGuid().ToByteArray()) + Convert.ToHexStringLower(Guid.NewGuid().ToByteArray()),
            true,
            "2026-10",
            "2026-09",
            null,
            Now);
        context.TenantRegistrations.Add(registration);
        await Task.CompletedTask;

        return registration.Id;
    }
}
