﻿using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformTenantChangeDatabaseTests.cs
//
// PURPOSE:
// Company change requests against SQL Server (B12).
//
// RESPONSIBILITIES:
// - Prove a request, its proof and the projection round-trip
// - Prove the database allows one pending request per tenant
// - Prove approval writes the new details and the history row
//
// IMPORTANT NOTES:
// - Runs only when VIXLY_TEST_SQL_SERVER is set
// =========================================================
public sealed class PlatformTenantChangeDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private static readonly byte[] Png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3 };

    private readonly PlatformDatabaseFixture _database;

    public PlatformTenantChangeDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task Request_RoundTrips_WithProofAndProjection()
    {
        PlatformTenant tenant = await NewTenantAsync();
        PlatformTenantChangeRequest request = Submit(tenant);

        await using (PlatformDbContext context = _database.NewContext())
        {
            new PlatformTenantChangeRepository(context).Add(request);
            Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformTenantChangeRepository repository = new(context);
            PlatformTenantChangeItem item = (await repository.GetItemAsync(request.Id, CancellationToken.None))!;
            Assert.Equal(("Northwind Freight Inc.", "Northwind Logistics Ltd.", "Unit 4"), (item.Previous.LegalName, item.Proposed.LegalName, item.Proposed.AddressLine2));
            Assert.Equal((tenant.TenantCode, "image/png", Png.Length), (item.TenantCode, item.ProofContentType, item.ProofSizeBytes));
            Assert.Equal(request.Id, (await repository.GetPendingAsync(tenant.Id, CancellationToken.None))!.Id);
            Assert.Equal(Png, (await repository.GetProofAsync(request.Id, CancellationToken.None))!.Content);

            PagedResult<PlatformTenantChangeItem> pending = await repository.ListAsync(new PlatformTenantChangeFilter(PlatformTenantChangeStatus.Pending, tenant.Id), PagedRequest.First, CancellationToken.None);
            Assert.Equal(request.Id, Assert.Single(pending.Items).RequestId);
        }
    }

    [SqlServerFact]
    public async Task SecondPendingRequest_ForOneTenant_IsRefused()
    {
        PlatformTenant tenant = await NewTenantAsync();

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.TenantChangeRequests.Add(Submit(tenant));
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext racer = _database.NewContext();
        racer.TenantChangeRequests.Add(Submit(tenant));
        Assert.False(await new PlatformUnitOfWork(racer).SaveChangesAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task Approval_WritesTheNewDetails_AndTheHistoryRow()
    {
        PlatformTenant created = await NewTenantAsync();
        PlatformTenantChangeRequest submitted = Submit(created);

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.TenantChangeRequests.Add(submitted);
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformTenant tenant = await context.Tenants.SingleAsync(item => item.Id == created.Id);
            PlatformTenantChangeRequest request = (await new PlatformTenantChangeRepository(context).GetAsync(submitted.Id, CancellationToken.None))!;
            context.TenantLifecycleEvents.Add(request.Approve(tenant, "Checked.", _database.ActorUserId, Now.AddHours(1)));
            Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformTenant tenant = await context.Tenants.AsNoTracking().SingleAsync(item => item.Id == created.Id);
            Assert.Equal("Northwind Logistics Ltd.", tenant.Company.LegalName);
            Assert.True(await context.TenantLifecycleEvents.AnyAsync(item => item.PlatformTenantId == created.Id && item.EventCode == PlatformTenantEventCodes.CompanyChanged));
            PlatformTenantChangeItem item = (await new PlatformTenantChangeRepository(context).GetLatestItemAsync(created.Id, CancellationToken.None))!;
            Assert.Equal((PlatformTenantChangeStatus.Approved, "Checked."), (item.Status, item.DecisionNote));
            Assert.NotNull(item.DecidedByName);
        }
    }

    private static PlatformTenantChangeRequest Submit(PlatformTenant tenant)
    {
        return PlatformTenantChangeRequest.Submit(
            tenant,
            PlatformCompanyDetails.Create(
                "Northwind Logistics Ltd.",
                null,
                EmailAddress.Create("hello@northwind.example"),
                PhoneNumber.Create("+1 514 555 0199"),
                PlatformPostalAddress.Create("2 Dock Street", "Unit 4", "Laval", null, null, "CA")),
            "We merged.",
            "certificate.png",
            Png,
            "ops-user-1",
            "Grace Hopper",
            Now);
    }

    private async Task<PlatformTenant> NewTenantAsync()
    {
        PlatformTenantCreation creation = PlatformTenant.CreateByStaff(
            "TEN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            "change-" + Guid.NewGuid().ToString("N")[..8],
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

        await using PlatformDbContext context = _database.NewContext();
        context.Tenants.Add(creation.Tenant);
        await context.SaveChangesAsync();
        return creation.Tenant;
    }
}
